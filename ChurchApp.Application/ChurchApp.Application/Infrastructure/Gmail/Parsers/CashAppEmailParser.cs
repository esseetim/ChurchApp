using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using ChurchApp.Application.Domain.Transactions;
using ChurchApp.Primitives.Donations;
using ErrorOr;
using HtmlAgilityPack;

namespace ChurchApp.Application.Infrastructure.Gmail.Parsers;

/// <summary>
/// Parses transaction notification emails from CashApp (cash@squareup.com)
/// </summary>
public sealed partial class CashAppEmailParser : IEmailParser
{
    public TransactionProvider Provider => TransactionProvider.CashApp;

    /// <summary>
    /// Parses CashApp transaction email
    /// </summary>
    /// <remarks>
    /// CashApp emails typically have:
    /// - Subject: "{Name} sent you ${amount}"
    /// - Body contains: sender $cashtag, amount, date, memo (optional), transaction ID
    /// </remarks>
    public ErrorOr<ParsedTransactionData> Parse(string emailBody, string emailSubject, string gmailMessageId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(emailBody);
        ArgumentException.ThrowIfNullOrWhiteSpace(emailSubject);
        ArgumentException.ThrowIfNullOrWhiteSpace(gmailMessageId);

        // Try HTML parsing first (most CashApp emails are HTML)
        var htmlResult = TryParseHtml(emailBody);
        if (htmlResult.IsError)
        {
            // Fallback to plain text parsing
            var textResult = TryParsePlainText(emailBody, emailSubject);
            if (textResult.IsError)
                return Error.Validation("CashApp.ParseFailed", "Failed to parse CashApp email");
            
            return BuildResult(textResult.Value, emailBody, gmailMessageId);
        }

        return BuildResult(htmlResult.Value, emailBody, gmailMessageId);
    }

    /// <summary>
    /// Attempts to parse HTML-formatted CashApp email
    /// </summary>
    private ErrorOr<TransactionFields> TryParseHtml(string htmlBody)
    {
        try
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(htmlBody);

            // CashApp emails have structured HTML with specific classes/IDs
            // Amount is typically in a prominent div
            var amountNode = doc.DocumentNode.SelectSingleNode("//*[contains(@class, 'amount') or contains(text(), '$')]");
            var amountMatch = AmountRegex().Match(amountNode?.InnerText ?? string.Empty);
            if (!amountMatch.Success)
                return Error.Validation("CashApp.Html.NoAmount", "Could not find amount in HTML");

            if (!decimal.TryParse(amountMatch.Groups[1].Value, NumberStyles.Currency, CultureInfo.InvariantCulture, out var amount))
                return Error.Validation("CashApp.Html.InvalidAmount", "Invalid amount format");

            // Sender name and $cashtag
            var senderNode = doc.DocumentNode.SelectSingleNode("//*[contains(@class, 'sender') or contains(text(), 'from')]");
            var senderText = senderNode?.InnerText ?? string.Empty;
            var cashtagMatch = CashtagRegex().Match(senderText);
            var cashtag = cashtagMatch.Success ? cashtagMatch.Value : null;

            // Extract sender name (remove $cashtag if present)
            var senderName = senderText.Replace(cashtag ?? string.Empty, string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(senderName))
                return Error.Validation("CashApp.Html.NoSender", "Could not find sender name");

            // Date (typically "Jan 15, 2026" format)
            var dateNode = doc.DocumentNode.SelectSingleNode("//*[contains(@class, 'date')]");
            var dateText = dateNode?.InnerText.Trim() ?? string.Empty;
            if (!TryParseDate(dateText, out var transactionDate))
                transactionDate = DateOnly.FromDateTime(DateTime.UtcNow); // Fallback to today

            // Memo/Note (optional)
            var memoNode = doc.DocumentNode.SelectSingleNode("//*[contains(@class, 'note') or contains(@class, 'memo')]");
            var memo = memoNode?.InnerText.Trim();

            // Transaction ID - try to find in various locations
            var txIdNode = doc.DocumentNode.SelectSingleNode("//*[contains(@class, 'transaction-id') or contains(text(), '#')]");
            var txId = txIdNode?.InnerText.Trim();
            
            // If no explicit transaction ID, generate one from available data
            txId ??= GenerateTransactionId(senderName, amount, transactionDate);

            return new TransactionFields(txId, senderName, cashtag, amount, transactionDate, memo);
        }
        catch (Exception ex)
        {
            return Error.Failure("CashApp.Html.ParseError", $"HTML parsing failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Attempts to parse plain text CashApp email
    /// </summary>
    private ErrorOr<TransactionFields> TryParsePlainText(string plainText, string subject)
    {
        // Subject typically: "{Name} sent you ${amount}"
        var subjectMatch = SubjectAmountRegex().Match(subject);
        if (!subjectMatch.Success)
            return Error.Validation("CashApp.Text.NoSubjectMatch", "Subject doesn't match expected format");

        var senderName = subjectMatch.Groups[1].Value.Trim();
        if (!decimal.TryParse(subjectMatch.Groups[2].Value, NumberStyles.Currency, CultureInfo.InvariantCulture, out var amount))
            return Error.Validation("CashApp.Text.InvalidAmount", "Invalid amount in subject");

        // Try to find $cashtag in body
        var cashtagMatch = CashtagRegex().Match(plainText);
        var cashtag = cashtagMatch.Success ? cashtagMatch.Value : null;

        // Try to find date
        var dateMatch = DatePatternRegex().Match(plainText);
        var transactionDate = dateMatch.Success && TryParseDate(dateMatch.Value, out var parsed)
            ? parsed
            : DateOnly.FromDateTime(DateTime.UtcNow);

        // Try to find memo (lines containing "For:" or "Note:")
        var memoMatch = MemoPatternRegex().Match(plainText);
        var memo = memoMatch.Success ? memoMatch.Groups[1].Value.Trim() : null;

        // Generate transaction ID from available data
        var txId = GenerateTransactionId(senderName, amount, transactionDate);

        return new TransactionFields(txId, senderName, cashtag, amount, transactionDate, memo);
    }

    /// <summary>
    /// Tries to parse various date formats CashApp uses
    /// </summary>
    private static bool TryParseDate(string dateText, out DateOnly date)
    {
        // Common formats: "Jan 15, 2026", "January 15", "1/15/2026"
        string[] formats =
        [
            "MMM d, yyyy",
            "MMMM d, yyyy",
            "M/d/yyyy",
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
    /// Generates a deterministic transaction ID when one isn't explicitly provided
    /// </summary>
    private static string GenerateTransactionId(string senderName, decimal amount, DateOnly date)
    {
        // Format: CASHAPP-{SENDER}-{AMOUNT}-{DATE}
        var sanitizedSender = string.Concat(senderName.Where(char.IsLetterOrDigit)).ToUpperInvariant();
        return $"CASHAPP-{sanitizedSender}-{amount:F2}-{date:yyyyMMdd}";
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
            provider = "CashApp",
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
            Provider = TransactionProvider.CashApp,
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

    [GeneratedRegex(@"\$[a-zA-Z0-9_]{3,20}", RegexOptions.Compiled)]
    private static partial Regex CashtagRegex();

    [GeneratedRegex(@"(.+?)\s+sent you\s+\$?([\d,]+\.?\d{0,2})", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex SubjectAmountRegex();

    [GeneratedRegex(@"(?:Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)[a-z]*\s+\d{1,2}(?:,\s*\d{4})?", RegexOptions.Compiled)]
    private static partial Regex DatePatternRegex();

    [GeneratedRegex(@"(?:For|Note):\s*(.+)", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex MemoPatternRegex();
}
