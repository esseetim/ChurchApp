using System.Text;
using ChurchApp.Application.Domain.Transactions;
using ChurchApp.Application.Features.Transactions;
using ChurchApp.Application.Infrastructure.Gmail.Parsers;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace ChurchApp.Application.Infrastructure.Gmail;

/// <summary>
/// Background service that polls Gmail for transaction emails and extracts them into RawTransactions
/// </summary>
/// <remarks>
/// Design principles:
/// - Resilience: Circuit breaker prevents cascading failures
/// - Idempotency: Checks ProviderTransactionId before inserting
/// - Separation of Concerns: Parsing delegated to IEmailParser implementations
/// - Observability: Structured logging for all operations
/// </remarks>
public sealed class GmailExtractionWorker : BackgroundService
{
    private readonly ILogger<GmailExtractionWorker> _logger;
    private readonly GmailWorkerOptions _options;
    private readonly IDbContextFactory<ChurchAppDbContext> _dbContextFactory;
    private readonly IEnumerable<IEmailParser> _parsers;
    private readonly IIntegrationEventHandler<RawTransactionExtractedEvent> _eventHandler;
    private readonly ResiliencePipeline _resiliencePipeline;
    private GmailService? _gmailService;

    public GmailExtractionWorker(
        ILogger<GmailExtractionWorker> logger,
        IOptions<GmailWorkerOptions> options,
        IDbContextFactory<ChurchAppDbContext> dbContextFactory,
        IEnumerable<IEmailParser> parsers,
        IIntegrationEventHandler<RawTransactionExtractedEvent> eventHandler)
    {
        _logger = logger;
        _options = options.Value;
        _dbContextFactory = dbContextFactory;
        _parsers = parsers;
        _eventHandler = eventHandler;

        // Build resilience pipeline with circuit breaker
        _resiliencePipeline = new ResiliencePipelineBuilder()
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = 0.5,
                SamplingDuration = TimeSpan.FromSeconds(30),
                MinimumThroughput = 3,
                BreakDuration = TimeSpan.FromMinutes(5),
                OnOpened = args =>
                {
                    _logger.LogError(args.Outcome.Exception, "Circuit breaker opened. Will retry after {BreakDuration}", TimeSpan.FromMinutes(5));
                    return ValueTask.CompletedTask;
                },
                OnClosed = args =>
                {
                    _logger.LogInformation("Circuit breaker closed. Resuming normal operations");
                    return ValueTask.CompletedTask;
                },
                OnHalfOpened = args =>
                {
                    _logger.LogInformation("Circuit breaker half-open. Testing connection");
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    /// <summary>
    /// Initializes the Gmail service with proper authentication
    /// </summary>
    private async Task InitializeGmailServiceAsync(CancellationToken cancellationToken)
    {
        if (_gmailService != null)
            return;

        _logger.LogInformation("Initializing Gmail API service");

        GoogleCredential credential;

        // Load credentials from file or default application credentials
        if (!string.IsNullOrWhiteSpace(_options.CredentialsPath) && File.Exists(_options.CredentialsPath))
        {
            _logger.LogInformation("Loading credentials from file: {Path}", _options.CredentialsPath);
            
            await using var stream = new FileStream(_options.CredentialsPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            credential = await GoogleCredential.FromStreamAsync(stream, cancellationToken);

            // If service account, set up domain-wide delegation
            if (!string.IsNullOrWhiteSpace(_options.UserEmail) && credential is { IsCreateScopedRequired: true })
            {
                _logger.LogInformation("Setting up domain-wide delegation for user: {Email}", _options.UserEmail);
                credential = credential
                    .CreateScoped(GmailService.Scope.GmailReadonly)
                    .CreateWithUser(_options.UserEmail);
            }
        }
        else
        {
            _logger.LogInformation("Using default application credentials");
            credential = await GoogleCredential.GetApplicationDefaultAsync(cancellationToken);
        }

        // Ensure read-only scope
        if (credential.IsCreateScopedRequired)
        {
            credential = credential.CreateScoped(GmailService.Scope.GmailReadonly);
        }

        _gmailService = new GmailService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "ChurchApp Transaction Extractor"
        });

        _logger.LogInformation("Gmail API service initialized successfully");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Gmail Extraction Worker starting. Polling interval: {Interval}", _options.PollingInterval);

        // Wait a bit before starting to allow app to fully initialize
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _resiliencePipeline.ExecuteAsync(async ct =>
                {
                    await ProcessEmailBatchAsync(ct);
                }, stoppingToken);
            }
            catch (BrokenCircuitException ex)
            {
                _logger.LogWarning(ex, "Circuit breaker is open. Skipping this cycle");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in Gmail extraction worker");
            }

            // Wait for next polling interval
            await Task.Delay(_options.PollingInterval, stoppingToken);
        }

        _logger.LogInformation("Gmail Extraction Worker stopping");
    }

    /// <summary>
    /// Processes a batch of unread transaction emails
    /// </summary>
    private async Task ProcessEmailBatchAsync(CancellationToken cancellationToken)
    {
        await InitializeGmailServiceAsync(cancellationToken);

        if (_gmailService == null)
        {
            _logger.LogError("Gmail service is not initialized");
            return;
        }

        _logger.LogDebug("Starting email batch processing. Query: {Query}", _options.EmailQuery);

        // Query for unread transaction emails
        var listRequest = _gmailService.Users.Messages.List("me");
        listRequest.Q = $"{_options.EmailQuery} is:unread";
        listRequest.MaxResults = _options.MaxMessagesPerBatch;

        var listResponse = await listRequest.ExecuteAsync(cancellationToken);

        if (listResponse.Messages == null || listResponse.Messages.Count == 0)
        {
            _logger.LogDebug("No new transaction emails found");
            return;
        }

        _logger.LogInformation("Found {Count} unread transaction email(s)", listResponse.Messages.Count);

        var processed = 0;
        var failed = 0;

        foreach (var messageRef in listResponse.Messages)
        {
            try
            {
                await ProcessSingleEmailAsync(messageRef.Id, cancellationToken);
                processed++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process email {MessageId}", messageRef.Id);
                failed++;
            }
        }

        _logger.LogInformation("Batch processing complete. Processed: {Processed}, Failed: {Failed}", processed, failed);
    }

    /// <summary>
    /// Processes a single email message
    /// </summary>
    private async Task ProcessSingleEmailAsync(string messageId, CancellationToken cancellationToken)
    {
        if (_gmailService == null)
            throw new InvalidOperationException("Gmail service is not initialized");

        // Fetch full message
        var getRequest = _gmailService.Users.Messages.Get("me", messageId);
        getRequest.Format = UsersResource.MessagesResource.GetRequest.FormatEnum.Full;
        var message = await getRequest.ExecuteAsync(cancellationToken);

        // Extract email content
        var subject = message.Payload.Headers.FirstOrDefault(h => h.Name.Equals("Subject", StringComparison.OrdinalIgnoreCase))?.Value ?? string.Empty;
        var from = message.Payload.Headers.FirstOrDefault(h => h.Name.Equals("From", StringComparison.OrdinalIgnoreCase))?.Value ?? string.Empty;
        var body = ExtractEmailBody(message.Payload);

        _logger.LogDebug("Processing email. Subject: {Subject}, From: {From}", subject, from);

        // Determine which parser to use
        var parser = DetermineParser(from);
        if (parser == null)
        {
            _logger.LogWarning("No parser found for email from {From}. Skipping", from);
            return;
        }

        // Parse email content
        var parseResult = parser.Parse(body, subject, messageId);
        if (parseResult.IsError)
        {
            _logger.LogError("Failed to parse email {MessageId}: {Errors}",
                messageId,
                string.Join(", ", parseResult.Errors.Select(e => $"{e.Code}: {e.Description}")));
            return;
        }

        var parsedData = parseResult.Value;

        // Check for duplicate (idempotency)
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        
        var exists = await dbContext.RawTransactions
            .AnyAsync(rt => rt.ProviderTransactionId == parsedData.ProviderTransactionId, cancellationToken);

        if (exists)
        {
            _logger.LogInformation("Transaction {TxId} already exists. Skipping duplicate", parsedData.ProviderTransactionId);
            
            // Still mark as read since we've seen it
            if (_options.MarkAsRead)
                await MarkAsReadAsync(messageId, cancellationToken);
            
            return;
        }

        // Create RawTransaction entity
        var rawTransaction = RawTransaction.Create(
            parsedData.ProviderTransactionId,
            parsedData.Provider,
            parsedData.SenderName,
            parsedData.SenderHandle,
            parsedData.Amount,
            parsedData.TransactionDate,
            parsedData.Memo,
            parsedData.RawContentJson,
            parsedData.GmailMessageId);

        dbContext.RawTransactions.Add(rawTransaction);
        await dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created RawTransaction {TxId} for {Amount} from {Sender}",
            parsedData.ProviderTransactionId,
            parsedData.Amount,
            parsedData.SenderName);

        // Publish integration event for automatic resolution
        if (_options.EnableAutomaticResolution)
        {
            var @event = new RawTransactionExtractedEvent(rawTransaction.Id, rawTransaction.ProviderTransactionId);
            await _eventHandler.HandleAsync(@event, cancellationToken);
        }

        // Mark email as read
        if (_options.MarkAsRead)
        {
            await MarkAsReadAsync(messageId, cancellationToken);
        }
    }

    /// <summary>
    /// Extracts email body content (HTML or plain text)
    /// </summary>
    private static string ExtractEmailBody(MessagePart payload)
    {
        if (payload.Body?.Data != null)
        {
            var bodyBytes = Convert.FromBase64String(payload.Body.Data.Replace('-', '+').Replace('_', '/'));
            return Encoding.UTF8.GetString(bodyBytes);
        }

        // Multi-part message
        if (payload.Parts != null)
        {
            // Prefer HTML, fallback to plain text
            var htmlPart = payload.Parts.FirstOrDefault(p => p.MimeType == "text/html");
            if (htmlPart?.Body?.Data != null)
            {
                var bodyBytes = Convert.FromBase64String(htmlPart.Body.Data.Replace('-', '+').Replace('_', '/'));
                return Encoding.UTF8.GetString(bodyBytes);
            }

            var textPart = payload.Parts.FirstOrDefault(p => p.MimeType == "text/plain");
            if (textPart?.Body?.Data != null)
            {
                var bodyBytes = Convert.FromBase64String(textPart.Body.Data.Replace('-', '+').Replace('_', '/'));
                return Encoding.UTF8.GetString(bodyBytes);
            }
        }

        return string.Empty;
    }

    /// <summary>
    /// Determines which parser to use based on email sender
    /// </summary>
    private IEmailParser? DetermineParser(string from)
    {
        if (from.Contains("cash@squareup.com", StringComparison.OrdinalIgnoreCase))
            return _parsers.FirstOrDefault(p => p.Provider == TransactionProvider.CashApp);

        if (from.Contains("no-reply@zellepay.com", StringComparison.OrdinalIgnoreCase) ||
            from.Contains("zelle", StringComparison.OrdinalIgnoreCase))
            return _parsers.FirstOrDefault(p => p.Provider == TransactionProvider.Zelle);

        return null;
    }

    /// <summary>
    /// Marks an email as read
    /// </summary>
    private async Task MarkAsReadAsync(string messageId, CancellationToken cancellationToken)
    {
        if (_gmailService == null)
            return;

        try
        {
            var modifyRequest = new ModifyMessageRequest
            {
                RemoveLabelIds = new List<string> { "UNREAD" }
            };

            await _gmailService.Users.Messages.Modify(modifyRequest, "me", messageId).ExecuteAsync(cancellationToken);
            _logger.LogDebug("Marked email {MessageId} as read", messageId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to mark email {MessageId} as read", messageId);
        }
    }

    public override void Dispose()
    {
        _gmailService?.Dispose();
        base.Dispose();
    }
}
