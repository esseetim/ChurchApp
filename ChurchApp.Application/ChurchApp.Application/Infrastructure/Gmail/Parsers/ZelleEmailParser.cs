using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using ChurchApp.Application.Domain.Transactions;
using ChurchApp.Primitives.Donations;
using ErrorOr;
using HtmlAgilityPack;

namespace ChurchApp.Application.Infrastructure.Gmail.Parsers;

/// <summary>
/// Parses transaction notification emails from Zelle (no-reply@zellepay.com)
/// </summary>
public sealed partial class ZelleEmailParser : IEmailParser
{
    public TransactionProvider Provider => TransactionProvider.Zelle;

    /// <summary>
    /// Parses Zelle transaction email
    /// </summary>
    /// <remarks>
    /// Zelle emails typically have:
    /// - Subject: "You've received money from {Name}"
    /// - Body contains: sender email/phone, amount, date, memo (optional)
    /// </remarks>
    public ErrorOr<ParsedTransactionData> Parse(string emailBody, string emailSubject, string gmailMessageId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(emailBody);
        ArgumentException.ThrowIfNullOrWhiteSpace(emailSubject);
        ArgumentException.ThrowIfNullOrWhiteSpace(gmailMessageId);

        // Try HTML parsing first (most Zelle emails are HTML)
        var htmlResult = TryParseHtml(emailBody, emailSubject);
        if (htmlResult.IsError)
        {
            // Fallback to plain text parsing
            var textResult = TryParsePlainText(emailBody, emailSubject);
            if (textResult.IsError)
                return Error.Validation("Zelle.ParseFailed", "Failed to parse Zelle email");
            
            return BuildResult(textResult.Value, emailBody, gmailMessageId);
        }

        return BuildResult(htmlResult.Value, emailBody, gmailMessageId);
    }

    /// <summary>
    /// Attempts to parse HTML-formatted Zelle email
    /// </summary>
    private ErrorOr<TransactionFields> TryParseHtml(string htmlBody, string subject)
    {
        try
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(htmlBody);

            // Amount is usually in a prominent div or table cell
            var amountNode = doc.DocumentNode.SelectSingleNode("//*[contains(@class, 'amount') or contains(text(), '$')]");
            var amountText = amountNode?.InnerText ?? string.Empty;
            var amountMatch = AmountRegex().Match(amountText);
            
            if (!amountMatch.Success)
                return Error.Validation("Zelle.Html.NoAmount", "Could not find amount in HTML");

            if (!decimal.TryParse(amountMatch.Groups[1].Value.Replace(",", string.Empty), NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
                return Error.Validation("Zelle.Html.InvalidAmount", "Invalid amount format");

            // Sender name (from subject or body)
            var senderName = ExtractSenderNameFromSubject(subject);
            if (string.IsNullOrWhiteSpace(senderName))
            {
                var senderNode = doc.DocumentNode.SelectSingleNode("//*[contains(@class, 'sender') or contains(text(), 'from')]");
                senderName = senderNode?.InnerText.Trim() ?? "Unknown";
            }

            // Sender handle (email or phone)
            var handleNode = doc.DocumentNode.SelectSingleNode("//*[contains(@class, 'email') or contains(@class, 'phone')]");
            var handleText = handleNode?.InnerText ?? htmlBody;
            var handle = ExtractHandle(handleText);

            // Date
            var dateNode = doc.DocumentNode.SelectSingleNode("//*[contains(@class, 'date') or contains(text(), 'on')]");
            var dateText = dateNode?.InnerText.Trim() ?? string.Empty;
            if (!TryParseDate(dateText, out var transactionDate))
                transactionDate = DateOnly.FromDateTime(DateTime.UtcNow);

            // Memo (optional - Zelle calls it "message")
            var memoNode = doc.DocumentNode.SelectSingleNode("//*[contains(@class, 'message') or contains(@class, 'memo')]");
            var memo = memoNode?.InnerText.Trim();
            if (string.IsNullOrWhiteSpace(memo))
                memo = null;

            // Generate transaction ID (Zelle doesn't always provide one in emails)
            var txId = GenerateTransactionId(senderName, handle, amount, transactionDate);

            return new TransactionFields(txId, senderName, handle, amount, transactionDate, memo);
        }
        catch (Exception ex)
        {
            return Error.Failure("Zelle.Html.ParseError", $"HTML parsing failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Attempts to parse plain text Zelle email
    /// </summary>
    private ErrorOr<TransactionFields> TryParsePlainText(string plainText, string subject)
    {
        // Subject typically: "You've received money from {Name}"
        var senderName = ExtractSenderNameFromSubject(subject);
        if (string.IsNullOrWhiteSpace(senderName))
            return Error.Validation("Zelle.Text.NoSender", "Could not extract sender from subject");

        // Find amount in body
        var amountMatch = AmountRegex().Match(plainText);
        if (!amountMatch.Success)
            return Error.Validation("Zelle.Text.NoAmount", "Could not find amount in email body");

        if (!decimal.TryParse(amountMatch.Groups[1].Value.Replace(",", string.Empty), NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
            return Error.Validation("Zelle.Text.InvalidAmount", "Invalid amount format");

        // Extract handle (email or phone)
        var handle = ExtractHandle(plainText);

        // Try to find date
        var dateMatch = DatePatternRegex().Match(plainText);
        var transactionDate = dateMatch.Success && TryParseDate(dateMatch.Value, out var parsed)
            ? parsed
            : DateOnly.FromDateTime(DateTime.UtcNow);

        // Try to find memo
        var memoMatch = MemoPatternRegex().Match(plainText);
        var memo = memoMatch.Success ? memoMatch.Groups[1].Value.Trim() : null;

        // Generate transaction ID
        var txId = GenerateTransactionId(senderName, handle, amount, transactionDate);

        return new TransactionFields(txId, senderName, handle, amount, transactionDate, memo);
    }

    /// <summary>
    /// Extracts sender name from Zelle subject line
    /// </summary>
    private static string ExtractSenderNameFromSubject(string subject)
    {
        // Pattern: "You've received money from {Name}"
        var match = SubjectSenderRegex().Match(subject);
        return match.Success ? match.Groups[1].Value.Trim() : string.Empty;
    }

    /// <summary>
    /// Extracts email or phone number from text
    /// </summary>
    private static string? ExtractHandle(string text)
    {
        // Try email first
        var emailMatch = EmailRegex().Match(text);
        if (emailMatch.Success)
            return emailMatch.Value;

        // Try phone number
        var phoneMatch = PhoneRegex().Match(text);
        if (phoneMatch.Success)
        {
            // Normalize phone to E.164 format
            var digits = string.Concat(phoneMatch.Value.Where(char.IsDigit));
            if (digits.Length == 10)
                return $"+1{digits}"; // Assume US
            if (digits.Length == 11 && digits[0] == '1')
                return $"+{digits}";
            return $"+{digits}";
        }

        return null;
    }

    /// <summary>
    /// Tries to parse various date formats Zelle uses
    /// </summary>
    private static bool TryParseDate(string dateText, out DateOnly date)
    {
        string[] formats =
        [
            "MMM d, yyyy",
            "MMMM d, yyyy",
            "M/d/yyyy",
            "yyyy-MM-dd",
            "MMM d",
            "MMMM d"
        ];

        foreach (var format in formats)
        {
            if (DateTime.TryParseExact(dateText, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
            {
                date = DateOnly.FromDateTime(parsedDate);
                return true;
            }
        }

        date = default;
        return false;
    }

    /// <summary>
    /// Generates a deterministic transaction ID
    /// </summary>
    private static string GenerateTransactionId(string senderName, string? handle, decimal amount, DateOnly date)
    {
        // Format: ZELLE-{SENDER}-{HANDLE_HASH}-{AMOUNT}-{DATE}
        var sanitizedSender = string.Concat(senderName.Where(char.IsLetterOrDigit)).ToUpperInvariant();
        var handleHash = handle != null ? Math.Abs(handle.GetHashCode()).ToString("X8") : "NOHANDLE";
        return $"ZELLE-{sanitizedSender}-{handleHash}-{amount:F2}-{date:yyyyMMdd}";
    }

    /// <summary>
    /// Builds the final ParsedTransactionData result
    /// </summary>
    private ParsedTransactionData BuildResult(
        TransactionFields fields,
        string rawEmailBody,
        string gmailMessageId)
    {
        var amountResult = DonationAmount.Create(fields.Amount);
        if (amountResult.IsError)
            throw new InvalidOperationException($"Invalid amount: {fields.Amount}");

        var rawJson = JsonSerializer.Serialize(new
        {
            provider = "Zelle",
            senderName = fields.SenderName,
            senderHandle = fields.SenderHandle,
            amount = fields.Amount,
            date = fields.TransactionDate,
            memo = fields.Memo,
            transactionId = fields.TransactionId,
            rawBody = rawEmailBody,
            gmailMessageId
        });

        return new ParsedTransactionData
        {
            ProviderTransactionId = fields.TransactionId,
            Provider = TransactionProvider.Zelle,
            SenderName = fields.SenderName,
            SenderHandle = fields.SenderHandle,
            Amount = amountResult.Value,
            TransactionDate = fields.TransactionDate,
            Memo = fields.Memo,
            RawContentJson = rawJson,
            GmailMessageId = gmailMessageId
        };
    }

    /// <summary>
    /// Internal record to hold parsed fields
    /// </summary>
    private sealed record TransactionFields(
        string TransactionId,
        string SenderName,
        string? SenderHandle,
        decimal Amount,
        DateOnly TransactionDate,
        string? Memo);

    // Compiled regex patterns for performance
    [GeneratedRegex(@"\$?([\d,]+\.?\d{0,2})", RegexOptions.Compiled)]
    private static partial Regex AmountRegex();

    [GeneratedRegex(@"You'?ve received (?:money )?from\s+(.+?)(?:\.|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex SubjectSenderRegex();

    [GeneratedRegex(@"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}", RegexOptions.Compiled)]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"[\(\[]?\d{3}[\)\]]?[-.\s]?\d{3}[-.\s]?\d{4}", RegexOptions.Compiled)]
    private static partial Regex PhoneRegex();

    [GeneratedRegex(@"(?:Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)[a-z]*\s+\d{1,2}(?:,\s*\d{4})?", RegexOptions.Compiled)]
    private static partial Regex DatePatternRegex();

    [GeneratedRegex(@"(?:Message|Memo|Note):\s*(.+)", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex MemoPatternRegex();
}
