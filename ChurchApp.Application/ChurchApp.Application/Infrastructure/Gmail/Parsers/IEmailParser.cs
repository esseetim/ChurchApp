using ChurchApp.Application.Domain.Transactions;
using ChurchApp.Primitives.Donations;
using ErrorOr;

namespace ChurchApp.Application.Infrastructure.Gmail.Parsers;

/// <summary>
/// Defines the contract for parsing transaction emails from payment providers
/// </summary>
public interface IEmailParser
{
    /// <summary>
    /// The payment provider this parser handles
    /// </summary>
    TransactionProvider Provider { get; }
    
    /// <summary>
    /// Parses an email body (HTML or plain text) and extracts transaction data
    /// </summary>
    /// <param name="emailBody">The email body content</param>
    /// <param name="emailSubject">The email subject line</param>
    /// <param name="gmailMessageId">Gmail message ID for audit trail</param>
    /// <returns>Parsed transaction data or validation errors</returns>
    ErrorOr<ParsedTransactionData> Parse(string emailBody, string emailSubject, string gmailMessageId);
}

/// <summary>
/// Represents the extracted data from a transaction email
/// </summary>
public sealed record ParsedTransactionData
{
    public required string ProviderTransactionId { get; init; }
    public required TransactionProvider Provider { get; init; }
    public required string SenderName { get; init; }
    public string? SenderHandle { get; init; }
    public required DonationAmount Amount { get; init; }
    public required DateOnly TransactionDate { get; init; }
    public string? Memo { get; init; }
    public required string RawContentJson { get; init; }
    public required string GmailMessageId { get; init; }
}
