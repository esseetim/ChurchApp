using ChurchApp.Application.Domain.Donations;
using ChurchApp.Application.Domain.Obligations;
using ChurchApp.Application.Domain.Transactions;
using ChurchApp.Application.Features.Transactions.Classification;
using ChurchApp.Application.Features.Transactions.Repositories;
using ChurchApp.Application.Infrastructure;
using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChurchApp.Application.Features.Transactions;

/// <summary>
/// Handles the resolution of raw transactions into donations.
/// Follows Single Responsibility Principle - only concerns are matching and donation creation.
/// </summary>
public sealed class RawTransactionResolverHandler : IIntegrationEventHandler<RawTransactionExtractedEvent>
{
    private readonly ChurchAppDbContext _dbContext;
    private readonly IDonationAccountRepository _accountRepository;
    private readonly DonationTypeClassificationService _classificationService;
    private readonly ILogger<RawTransactionResolverHandler> _logger;

    public RawTransactionResolverHandler(
        ChurchAppDbContext dbContext,
        IDonationAccountRepository accountRepository,
        DonationTypeClassificationService classificationService,
        ILogger<RawTransactionResolverHandler> logger)
    {
        _dbContext = dbContext;
        _accountRepository = accountRepository;
        _classificationService = classificationService;
        _logger = logger;
    }

    public async Task<ErrorOr<Success>> HandleAsync(
        RawTransactionExtractedEvent @event,
        CancellationToken cancellationToken)
    {
        var rawTx = await LoadRawTransactionAsync(@event.RawTransactionId, cancellationToken);
        
        if (!ShouldProcess(rawTx))
        {
            _logger.LogDebug("Transaction {TxId} already processed or missing", @event.ProviderTransactionId);
            return Result.Success;
        }

        // Null-forgiving operator safe here because ShouldProcess ensures non-null
        var matchResult = await TryMatchAccountAsync(rawTx!, cancellationToken);
        
        if (matchResult.IsError)
        {
            await MarkUnmatchedAsync(rawTx!, cancellationToken);
            _logger.LogInformation(
                "Transaction {TxId} marked as unmatched - no account found for {Provider} {Handle}/{Name}",
                rawTx!.ProviderTransactionId,
                rawTx.Provider,
                rawTx.SenderHandle,
                rawTx.SenderName);
            return Result.Success;
        }

        var account = matchResult.Value;
        var donationSpec = BuildDonationSpecification(rawTx!, account);
        
        // Check for obligation match
        var obligationId = await TryMatchObligationAsync(
            account.MemberId,
            rawTx!.Memo,
            cancellationToken);

        var donation = await CreateDonationAsync(donationSpec, obligationId, cancellationToken);
        await MarkResolvedAsync(rawTx!, donation.Id, cancellationToken);

        _logger.LogInformation(
            "Transaction {TxId} resolved to donation {DonationId} for member {MemberId}",
            rawTx.ProviderTransactionId,
            donation.Id,
            account.MemberId);

        return Result.Success;
    }

    private async Task<RawTransaction?> LoadRawTransactionAsync(
        Guid rawTransactionId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.RawTransactions
            .FirstOrDefaultAsync(x => x.Id == rawTransactionId, cancellationToken);
    }

    private static bool ShouldProcess(RawTransaction? rawTx)
    {
        return rawTx is not null && rawTx.Status == RawTransactionStatus.Pending;
    }

    private async Task<ErrorOr<DonationAccount>> TryMatchAccountAsync(
        RawTransaction rawTx,
        CancellationToken cancellationToken)
    {
        var method = rawTx.Provider == TransactionProvider.CashApp
            ? DonationMethod.CashApp
            : DonationMethod.Zelle;

        var account = await _accountRepository.FindByHandleOrNameAsync(
            method,
            rawTx.SenderHandle,
            rawTx.SenderName,
            cancellationToken);

        return account is not null
            ? account
            : Error.NotFound("DonationAccount.NotFound", "No matching account found");
    }

    private DonationSpecification BuildDonationSpecification(
        RawTransaction rawTx,
        DonationAccount account)
    {
        var donationType = _classificationService.Classify(rawTx.Memo);
        var method = rawTx.Provider == TransactionProvider.CashApp
            ? DonationMethod.CashApp
            : DonationMethod.Zelle;

        return new DonationSpecification(
            MemberId: account.MemberId,
            DonationAccountId: account.Id,
            Type: donationType,
            Method: method,
            DonationDate: rawTx.TransactionDate,
            Amount: rawTx.Amount,
            IdempotencyKey: rawTx.ProviderTransactionId,
            EnteredBy: "system-auto-extractor",
            ServiceName: null,
            Notes: string.IsNullOrWhiteSpace(rawTx.Memo)
                ? $"Auto-extracted from {rawTx.Provider}"
                : $"Auto-extracted from {rawTx.Provider}. Memo: {rawTx.Memo}");
    }

    private async Task<Guid?> TryMatchObligationAsync(
        Guid memberId,
        string? memo,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(memo))
            return null;

        // Search for active obligations where memo contains the obligation title
        var obligation = await _dbContext.Set<FinancialObligation>()
            .Where(x => x.MemberId == memberId && x.Status == ObligationStatus.Active)
            .FirstOrDefaultAsync(
                x => EF.Functions.ILike(memo, $"%{x.Title}%"),
                cancellationToken);

        return obligation?.Id;
    }

    private async Task<Donation> CreateDonationAsync(
        DonationSpecification spec,
        Guid? obligationId,
        CancellationToken cancellationToken)
    {
        var donation = Donation.Create(
            memberId: spec.MemberId,
            donationAccountId: spec.DonationAccountId,
            type: spec.Type,
            method: spec.Method,
            donationDate: spec.DonationDate,
            amount: spec.Amount,
            idempotencyKey: spec.IdempotencyKey,
            enteredBy: spec.EnteredBy,
            serviceName: spec.ServiceName,
            notes: spec.Notes);

        // Override type and link obligation if matched
        if (obligationId.HasValue)
        {
            donation.ObligationId = obligationId;
            donation.Type = DonationType.PledgePayment; // Assume pledge for now
        }

        _dbContext.Donations.Add(donation);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return donation;
    }

    private async Task MarkUnmatchedAsync(RawTransaction rawTx, CancellationToken cancellationToken)
    {
        rawTx.MarkUnmatched();
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task MarkResolvedAsync(
        RawTransaction rawTx,
        Guid donationId,
        CancellationToken cancellationToken)
    {
        rawTx.MarkResolved(donationId);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Value object encapsulating donation creation parameters.
    /// Improves readability and reduces parameter count (Clean Code principle).
    /// </summary>
    private sealed record DonationSpecification(
        Guid MemberId,
        Guid DonationAccountId,
        DonationType Type,
        DonationMethod Method,
        DateOnly DonationDate,
        decimal Amount,
        string IdempotencyKey,
        string EnteredBy,
        string? ServiceName,
        string? Notes);
}
