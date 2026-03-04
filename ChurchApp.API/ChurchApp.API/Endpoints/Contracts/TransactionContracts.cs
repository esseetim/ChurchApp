using ChurchApp.Primitives.Donations;

namespace ChurchApp.API.Endpoints.Contracts;

/// <summary>
/// Request to query unmatched raw transactions
/// </summary>
public sealed record GetUnmatchedTransactionsRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}

/// <summary>
/// Response containing page of unmatched transactions
/// </summary>
public sealed record GetUnmatchedTransactionsResponse
{
    public required List<RawTransactionDto> Transactions { get; init; }
    public required int TotalCount { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }
}

/// <summary>
/// DTO for raw transaction
/// </summary>
public sealed record RawTransactionDto
{
    public required Guid Id { get; init; }
    public required string ProviderTransactionId { get; init; }
    public required string Provider { get; init; }
    public required string SenderName { get; init; }
    public string? SenderHandle { get; init; }
    public required DonationAmount Amount { get; init; }
    public required DateOnly TransactionDate { get; init; }
    public string? Memo { get; init; }
    public required string Status { get; init; }
    public required DateTime CreatedAtUtc { get; init; }
}

/// <summary>
/// Request to manually resolve a raw transaction
/// </summary>
public sealed record ResolveTransactionRequest
{
    public required Guid MemberId { get; init; }
    public Guid? DonationAccountId { get; init; }
    public bool SaveAsNewAccount { get; init; }
    public required DonationType DonationType { get; init; }
    public Guid? ObligationId { get; init; }
    public string? Notes { get; init; }
}

/// <summary>
/// Response after resolving a transaction
/// </summary>
public sealed record ResolveTransactionResponse
{
    public required Guid DonationId { get; init; }
    public Guid? DonationAccountId { get; init; }
    public required string Message { get; init; }
}

/// <summary>
/// Request to ignore a raw transaction
/// </summary>
public sealed record IgnoreTransactionRequest
{
    public string? Reason { get; init; }
}

/// <summary>
/// Response after ignoring a transaction
/// </summary>
public sealed record IgnoreTransactionResponse
{
    public required string Message { get; init; }
}
