namespace ChurchApp.Application.Domain.Transactions;

/// <summary>
/// Represents the processing status of a raw transaction extracted from email.
/// </summary>
public enum RawTransactionStatus
{
    /// <summary>
    /// Transaction has been extracted and is queued for processing
    /// </summary>
    Pending = 1,
    
    /// <summary>
    /// Transaction has been successfully matched to a member and donation created
    /// </summary>
    Resolved = 2,
    
    /// <summary>
    /// Transaction could not be automatically matched to any DonationAccount
    /// </summary>
    Unmatched = 3,
    
    /// <summary>
    /// Transaction was manually marked as duplicate or not a donation
    /// </summary>
    Ignored = 4
}
