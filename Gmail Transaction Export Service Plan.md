Here is a comprehensive architectural design and implementation plan to handle the automated Gmail extraction, idempotency, and resolution workflow.

### 1. Domain Model Updates

We need a staging entity, `RawTransaction`, to act as an immutable audit log of what was pulled from the email before it becomes a formalized `Donation`.

**New Enums:**

```csharp
namespace ChurchApp.Application.Domain.Transactions;

public enum TransactionProvider
{
    CashApp = 1,
    Zelle = 2
}

public enum RawTransactionStatus
{
    Pending = 1,     // Queued for processing
    Resolved = 2,    // Successfully matched and Donation created
    Unmatched = 3,   // Could not find a matching DonationAccount
    Ignored = 4      // Marked manually as duplicate/not a donation
}

```

**New `RawTransaction` Entity:**

```csharp
using ChurchApp.Application.Domain.Common;
using ChurchApp.Application.Domain.Donations;

namespace ChurchApp.Application.Domain.Transactions;

public class RawTransaction : IHasDomainEvents
{
    public Guid Id { get; set; }
    public required string MessageId { get; set; } // Gmail Message ID for idempotency
    public TransactionProvider Provider { get; set; }
    public required string SenderHandle { get; set; } // e.g., $cashtag or email/phone
    public string? SenderName { get; set; }
    public decimal Amount { get; set; }
    public DateTime TransactionDateUtc { get; set; }
    public required string RawContentJson { get; set; } // System.Text.Json serialized email data
    
    public RawTransactionStatus Status { get; set; }
    
    public Guid? ResolvedDonationId { get; set; }
    public Donation? ResolvedDonation { get; set; }

    public List<IDomainEvent> DomainEvents { get; } = [];

    public void MarkResolved(Guid donationId)
    {
        Status = RawTransactionStatus.Resolved;
        ResolvedDonationId = donationId;
    }

    public void MarkUnmatched()
    {
        Status = RawTransactionStatus.Unmatched;
    }
}

```

*Note: Create an EF Core configuration for this entity, specifically adding a unique index on `MessageId` to enforce idempotency at the database level.*

### 2. Events & Handlers

We will use an integration event to notify the system that a new raw transaction has landed and needs to be resolved.

**The Event:**

```csharp
namespace ChurchApp.Application.Features.Transactions;

public sealed record RawTransactionExtractedEvent(
    Guid RawTransactionId, 
    string MessageId) : IIntegrationEvent; 

```

**The Resolver Handler:**
This handler processes the event, attempts to match the handle, creates the donation if successful, and returns an `ErrorOr<Success>` result.

```csharp
using ChurchApp.Application.Domain.Donations;
using ChurchApp.Application.Domain.Transactions;
using ChurchApp.Application.Infrastructure;
using ErrorOr;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.Application.Features.Transactions;

public sealed class RawTransactionResolverHandler(ChurchAppDbContext dbContext) 
    : IIntegrationEventHandler<RawTransactionExtractedEvent>
{
    public async Task<ErrorOr<Success>> HandleAsync(RawTransactionExtractedEvent @event, CancellationToken cancellationToken)
    {
        var rawTx = await dbContext.RawTransactions
            .FirstOrDefaultAsync(x => x.Id == @event.RawTransactionId, cancellationToken);

        if (rawTx is null || rawTx.Status != RawTransactionStatus.Pending)
        {
            return Result.Success; // Already processed or missing, safely ignore
        }

        // 1. Attempt to find the matching DonationAccount
        var method = rawTx.Provider == TransactionProvider.CashApp ? DonationMethod.CashApp : DonationMethod.Zelle;
        
        var account = await dbContext.DonationAccounts
            .Include(x => x.Member)
            .FirstOrDefaultAsync(x => x.Method == method && x.Handle == rawTx.SenderHandle, cancellationToken);

        if (account is null)
        {
            rawTx.MarkUnmatched();
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result.Success; // Successfully processed, but unmatched. UI handles this later.
        }

        // 2. Create the Donation
        var donation = Donation.Create(
            memberId: account.MemberId,
            donationAccountId: account.Id,
            type: DonationType.GeneralOffering, // Default, can be adjusted manually later
            method: method,
            donationDate: DateOnly.FromDateTime(rawTx.TransactionDateUtc.ToLocalTime()),
            amount: rawTx.Amount,
            idempotencyKey: rawTx.MessageId, // Prevents duplicate donations
            enteredBy: "system-auto-extractor",
            serviceName: null,
            notes: $"Auto-extracted from {rawTx.Provider}");

        dbContext.Donations.Add(donation);
        
        // 3. Mark Resolved
        rawTx.MarkResolved(donation.Id);
        
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }
}

```

### 3. Third-Party Integration (Gmail API)

Create a dedicated .NET BackgroundService (Worker) that periodically polls the Gmail API using the `Google.Apis.Gmail.v1` client.

```csharp
public class GmailExtractionWorker(
    IServiceScopeFactory scopeFactory, 
    ILogger<GmailExtractionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ChurchAppDbContext>();
            var eventPublisher = scope.ServiceProvider.GetRequiredService<IIntegrationEventPublisher>();

            // 1. Query Gmail API
            // Query: "from:cash@squareup.com subject:sent you OR from:no-reply@zellepay.com"
            var messages = await FetchUnreadGmailMessagesAsync(stoppingToken);

            foreach (var msg in messages)
            {
                // Idempotency Check
                var exists = await dbContext.RawTransactions.AnyAsync(x => x.MessageId == msg.Id, stoppingToken);
                if (exists) continue;

                // 2. Parse Email Body (Regex / HTML Agility Pack)
                var extractedData = ParseTransactionData(msg);

                // 3. Persist RawTransaction
                var rawTx = new RawTransaction
                {
                    Id = Guid.CreateVersion7(),
                    MessageId = msg.Id,
                    Provider = extractedData.Provider,
                    SenderHandle = extractedData.Handle,
                    Amount = extractedData.Amount,
                    TransactionDateUtc = extractedData.Date,
                    RawContentJson = System.Text.Json.JsonSerializer.Serialize(msg),
                    Status = RawTransactionStatus.Pending
                };

                dbContext.RawTransactions.Add(rawTx);
                await dbContext.SaveChangesAsync(stoppingToken);

                // 4. Queue the event to resolve it asynchronously
                await eventPublisher.PublishAsync(
                    new RawTransactionExtractedEvent(rawTx.Id, rawTx.MessageId), 
                    stoppingToken);
            }

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}

```

### 4. UI / Web Application Integration

Because we separate the extraction from the resolution, transactions from unregistered members or unknown handles will fall into the `Unmatched` status. We need a UI to clean these up.

**"Inbox / Unresolved Transactions" Page:**

1. **Grid View:** Create a new page (`/inbox`) featuring a `RadzenDataGrid` bound to `RawTransactions` where `Status == Unmatched`.
2. **The Grid Columns:** Date, Provider, Sender Handle (e.g., $johnsmith), Sender Name (if available from email), and Amount.
3. **Resolution Action:** A button on each row labeled "Resolve".
4. **The Resolution Dialog:** * Clicking "Resolve" opens a Dialog.
* The dialog prompts the volunteer to select an existing `Member` (using your `MemberSelector` component) or Quick Create a new one.
* Once a Member is selected, the form has a checkbox (checked by default): *"Save {SenderHandle} as a permanent {Provider} account for this member"*.


5. **Submission:** When submitted, the API endpoint will:
* Create the `DonationAccount` for that member (so future emails auto-resolve).
* Create the `Donation` record.
* Update the `RawTransaction` status to `Resolved`.

## Corrections
This new structure from the emails provides a much richer dataset to work with. Using the provider's Transaction/Confirmation Number is significantly better for idempotency than the Gmail Message ID, as it protects against scenarios where the same receipt might be forwarded or sent twice. The "For" field (memo) is the missing link needed to automatically categorize the funds or attribute them to a pledge.

Here is how the architecture and implementation plan adapts to utilize these specific email fields.

### 1. Domain Model Refinements

The `RawTransaction` entity needs to be updated to capture the memo and use the provider's transaction ID as the primary source of truth.

**Updated `RawTransaction` Entity:**

```csharp
using ChurchApp.Application.Domain.Common;
using ChurchApp.Application.Domain.Donations;

namespace ChurchApp.Application.Domain.Transactions;

public class RawTransaction : IHasDomainEvents
{
    public Guid Id { get; set; }
    public required string ProviderTransactionId { get; set; } // CashApp Transaction # or Zelle Confirmation #
    public string? GmailMessageId { get; set; } // Kept for audit trailing back to the source email
    public TransactionProvider Provider { get; set; }
    
    public required string SenderName { get; set; } // Now required based on Zelle/CashApp behavior
    public string? SenderHandle { get; set; } // Might be null if the email only provides the name
    
    public decimal Amount { get; set; }
    public DateTime TransactionDate { get; set; }
    
    public string? Memo { get; set; } // The "For" field
    
    public required string RawContentJson { get; set; }
    public RawTransactionStatus Status { get; set; }
    
    public Guid? ResolvedDonationId { get; set; }
    public Donation? ResolvedDonation { get; set; }

    public List<IDomainEvent> DomainEvents { get; } = [];

    // ... MarkResolved and MarkUnmatched methods remain the same
}

```

*Note: The EF Core unique index must now be applied to `ProviderTransactionId` instead of `MessageId`.*

### 2. Enhancing the Gmail Extraction Worker

The background service will now extract these specific targets. Since both providers use a standard template, Regex or a lightweight HTML parser will work perfectly.

```csharp
// Inside GmailExtractionWorker.cs

// Idempotency check now uses ProviderTransactionId
var extractedData = ParseTransactionData(msg); // Parses Amount, SenderName, ProviderTxId, Date, Memo

var exists = await dbContext.RawTransactions
    .AnyAsync(x => x.ProviderTransactionId == extractedData.ProviderTransactionId, stoppingToken);

if (exists) continue;

var rawTx = new RawTransaction
{
    Id = Guid.CreateVersion7(),
    ProviderTransactionId = extractedData.ProviderTransactionId,
    GmailMessageId = msg.Id,
    Provider = extractedData.Provider,
    SenderName = extractedData.SenderName,
    SenderHandle = extractedData.SenderHandle, // If available
    Amount = extractedData.Amount,
    TransactionDate = extractedData.Date,
    Memo = extractedData.Memo, // Captured "For" field
    RawContentJson = System.Text.Json.JsonSerializer.Serialize(msg),
    Status = RawTransactionStatus.Pending
};

```

### 3. The Smart Resolver (Handling the "For" Field)

The `RawTransactionResolverHandler` must be updated to leverage the `Memo` and `SenderName`. Since the handler must return an `ErrorOr<Success>`, we can neatly encapsulate the matching logic.

```csharp
using ChurchApp.Application.Domain.Donations;
using ChurchApp.Application.Domain.Transactions;
using ChurchApp.Application.Infrastructure;
using ErrorOr;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.Application.Features.Transactions;

public sealed class RawTransactionResolverHandler(ChurchAppDbContext dbContext) 
    : IIntegrationEventHandler<RawTransactionExtractedEvent>
{
    public async Task<ErrorOr<Success>> HandleAsync(RawTransactionExtractedEvent @event, CancellationToken cancellationToken)
    {
        var rawTx = await dbContext.RawTransactions
            .FirstOrDefaultAsync(x => x.Id == @event.RawTransactionId, cancellationToken);

        if (rawTx is null || rawTx.Status != RawTransactionStatus.Pending) return Result.Success;

        var method = rawTx.Provider == TransactionProvider.CashApp ? DonationMethod.CashApp : DonationMethod.Zelle;

        // 1. Account Matching Strategy
        // Try Handle first, fallback to matching DisplayName/SenderName
        var account = await dbContext.DonationAccounts
            .Include(x => x.Member)
            .FirstOrDefaultAsync(x => x.Method == method && 
                (x.Handle == rawTx.SenderHandle || x.DisplayName == rawTx.SenderName), 
                cancellationToken);

        if (account is null)
        {
            rawTx.MarkUnmatched();
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result.Success; 
        }

        // 2. Intent Parsing Strategy (The "For" Field)
        var donationType = DetermineDonationType(rawTx.Memo);
        Guid? obligationId = await AttemptToMatchObligationAsync(account.MemberId, rawTx.Memo, dbContext, cancellationToken);

        // 3. Create Donation
        var donation = Donation.Create(
            memberId: account.MemberId,
            donationAccountId: account.Id,
            type: donationType,
            method: method,
            donationDate: DateOnly.FromDateTime(rawTx.TransactionDate),
            amount: rawTx.Amount,
            idempotencyKey: rawTx.ProviderTransactionId, // Mapped to the actual receipt number
            enteredBy: "system-auto-extractor",
            serviceName: null,
            notes: $"Auto-extracted. Memo: {rawTx.Memo}");

        // Manually assign ObligationId if found (since Create signature doesn't include it yet)
        if (obligationId.HasValue)
        {
            donation.ObligationId = obligationId;
            donation.Type = DonationType.PledgePayment; // Override if it matched an obligation
        }

        dbContext.Donations.Add(donation);
        rawTx.MarkResolved(donation.Id);
        
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }

    private static DonationType DetermineDonationType(string? memo)
    {
        if (string.IsNullOrWhiteSpace(memo)) return DonationType.GeneralOffering;
        
        var normalizedMemo = memo.ToLowerInvariant();
        if (normalizedMemo.Contains("tithe")) return DonationType.Tithe;
        if (normalizedMemo.Contains("building")) return DonationType.BuildingFund;
        
        return DonationType.GeneralOffering;
    }

    private static async Task<Guid?> AttemptToMatchObligationAsync(
        Guid memberId, string? memo, ChurchAppDbContext dbContext, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(memo)) return null;

        // Search active obligations for this member where the Memo contains the Obligation Title
        // (e.g., Memo: "For 2026 Mens Club Due" matches Title "2026 Mens Club Due")
        var obligation = await dbContext.Set<FinancialObligation>()
            .Where(x => x.MemberId == memberId && x.Status == ObligationStatus.Active)
            .FirstOrDefaultAsync(x => EF.Functions.ILike(memo, $"%{x.Title}%"), ct);

        return obligation?.Id;
    }
}

```

### 4. UI / Web Application Integration Updates

The manual resolution UI (`/inbox` page) becomes much more powerful with these fields:

1. **Grid Columns:** Update the grid to display `ProviderTransactionId`, `SenderName`, `Amount`, and heavily emphasize the `Memo` column so volunteers immediately know the context.
2. **Resolution Dialog Enhancements:**
* When a volunteer clicks "Resolve" on an unmatched transaction, the dialog pre-fills a mock Donation form based on the extracted data.
* Because the parser might have failed to auto-match an obligation, the UI should provide dropdowns for `DonationType` and `Obligation` so the volunteer can manually select "PledgePayment" and map it to the "New Roof Campaign".
* **The "Link Account" Checkbox:** When the volunteer selects a `Member` from the dropdown to resolve the transaction, the "Save as permanent account" checkbox will now bind `rawTx.SenderName` to the new `DonationAccount.DisplayName` field. This ensures that next month, when that same name appears in a Zelle email, the system automatically resolves it.

This architecture guarantees that the polling worker is lightning-fast and solely responsible for I/O and idempotency, 
while background dispatcher handles the business logic of linking entities, leaving the UI exclusively for human exception-handling.