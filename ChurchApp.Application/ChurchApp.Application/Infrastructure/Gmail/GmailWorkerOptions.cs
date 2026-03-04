namespace ChurchApp.Application.Infrastructure.Gmail;

/// <summary>
/// Configuration settings for the Gmail extraction worker
/// </summary>
public sealed class GmailWorkerOptions
{
    public const string Section = "GmailWorker";

    /// <summary>
    /// How often to poll Gmail for new transaction emails
    /// </summary>
    public TimeSpan PollingInterval { get; init; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Maximum number of emails to process per batch
    /// </summary>
    public int MaxMessagesPerBatch { get; init; } = 100;

    /// <summary>
    /// Whether to automatically attempt resolution of transactions
    /// </summary>
    public bool EnableAutomaticResolution { get; init; } = true;

    /// <summary>
    /// Gmail API credentials file path (service account JSON)
    /// Leave empty to use default application credentials
    /// </summary>
    public string? CredentialsPath { get; init; }

    /// <summary>
    /// Gmail user email address to impersonate (for domain-wide delegation)
    /// Required if using service account
    /// </summary>
    public string? UserEmail { get; init; }

    /// <summary>
    /// Gmail query for finding transaction emails
    /// Default: from:cash@squareup.com OR from:no-reply@zellepay.com
    /// </summary>
    public string EmailQuery { get; init; } = "from:cash@squareup.com OR from:no-reply@zellepay.com";

    /// <summary>
    /// Whether to mark processed emails as read
    /// </summary>
    public bool MarkAsRead { get; init; } = true;
}
