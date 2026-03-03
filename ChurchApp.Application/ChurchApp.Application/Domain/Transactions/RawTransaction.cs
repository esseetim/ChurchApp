using ChurchApp.Application.Domain.Common;
using ChurchApp.Application.Domain.Donations;

namespace ChurchApp.Application.Domain.Transactions;

/// <summary>
/// Represents an immutable audit log of a transaction extracted from email.
/// This entity follows the Event Sourcing pattern - it captures the raw state
/// before business logic is applied.
/// </summary>
/// <remarks>
/// Design principles:
/// - Immutability: Core fields cannot be changed after creation
/// - Audit Trail: Raw email content preserved for compliance
/// - Idempotency: ProviderTransactionId ensures no duplicates
/// - Single Responsibility: Only concerns are storage and status tracking
/// </remarks>
public class RawTransaction : IHasDomainEvents
{
    /// <summary>
    /// Unique identifier for this raw transaction record
    /// </summary>
    public Guid Id { get; private set; }
    
    /// <summary>
    /// The transaction ID from the payment provider (CashApp/Zelle).
    /// This is the source of truth for idempotency.
    /// </summary>
    public required string ProviderTransactionId { get; init; }
    
    /// <summary>
    /// Gmail Message ID for tracing back to the source email.
    /// Secondary identifier for audit purposes.
    /// </summary>
    public string? GmailMessageId { get; init; }
    
    /// <summary>
    /// The payment provider that sent the transaction notification
    /// </summary>
    public TransactionProvider Provider { get; init; }
    
    /// <summary>
    /// Sender's display name from the transaction email
    /// </summary>
    public required string SenderName { get; init; }
    
    /// <summary>
    /// Sender's handle ($cashtag, email, or phone) if available
    /// </summary>
    public string? SenderHandle { get; init; }
    
    /// <summary>
    /// Transaction amount in USD
    /// </summary>
    public decimal Amount { get; init; }
    
    /// <summary>
    /// Transaction date from the provider's email
    /// </summary>
    public DateOnly TransactionDate { get; init; }
    
    /// <summary>
    /// The "For" field or memo from the transaction
    /// </summary>
    public string? Memo { get; init; }
    
    /// <summary>
    /// Complete raw email content serialized as JSON for audit trail.
    /// Enables forensic analysis if business logic needs adjustment.
    /// </summary>
    public required string RawContentJson { get; init; }
    
    /// <summary>
    /// Current processing status of this transaction
    /// </summary>
    public RawTransactionStatus Status { get; private set; }
    
    /// <summary>
    /// FK to the Donation created from this transaction (if resolved)
    /// </summary>
    public Guid? ResolvedDonationId { get; private set; }
    
    /// <summary>
    /// Navigation property to the resolved donation
    /// </summary>
    public Donation? ResolvedDonation { get; private set; }
    
    /// <summary>
    /// Timestamp when this record was created
    /// </summary>
    public DateTime CreatedAtUtc { get; private set; }
    
    /// <summary>
    /// Domain events raised by this entity
    /// </summary>
    public List<IDomainEvent> DomainEvents { get; } = [];

    /// <summary>
    /// Private constructor for EF Core
    /// </summary>
    private RawTransaction() { }

    /// <summary>
    /// Factory method to create a new RawTransaction in Pending status.
    /// Following the Factory Pattern for controlled object creation.
    /// </summary>
    public static RawTransaction Create(
        string providerTransactionId,
        TransactionProvider provider,
        string senderName,
        string? senderHandle,
        decimal amount,
        DateOnly transactionDate,
        string? memo,
        string rawContentJson,
        string? gmailMessageId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerTransactionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(senderName);
        ArgumentException.ThrowIfNullOrWhiteSpace(rawContentJson);
        
        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than zero", nameof(amount));

        return new RawTransaction
        {
            Id = Guid.CreateVersion7(),
            ProviderTransactionId = providerTransactionId,
            GmailMessageId = gmailMessageId,
            Provider = provider,
            SenderName = senderName,
            SenderHandle = senderHandle,
            Amount = amount,
            TransactionDate = transactionDate,
            Memo = memo,
            RawContentJson = rawContentJson,
            Status = RawTransactionStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Marks this transaction as successfully resolved with a donation.
    /// </summary>
    /// <param name="donationId">The ID of the created donation</param>
    public void MarkResolved(Guid donationId)
    {
        if (Status != RawTransactionStatus.Pending)
            throw new InvalidOperationException($"Cannot resolve transaction in {Status} status");
        
        Status = RawTransactionStatus.Resolved;
        ResolvedDonationId = donationId;
    }

    /// <summary>
    /// Marks this transaction as unmatched (no DonationAccount found).
    /// </summary>
    public void MarkUnmatched()
    {
        if (Status != RawTransactionStatus.Pending)
            throw new InvalidOperationException($"Cannot mark as unmatched from {Status} status");
        
        Status = RawTransactionStatus.Unmatched;
    }

    /// <summary>
    /// Marks this transaction as ignored (manual override).
    /// </summary>
    public void MarkIgnored()
    {
        if (Status == RawTransactionStatus.Resolved)
            throw new InvalidOperationException("Cannot ignore a resolved transaction");
        
        Status = RawTransactionStatus.Ignored;
    }
}
