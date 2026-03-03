using System.Collections.Immutable;

namespace ChurchApp.Web.Blazor.Models;

public record CreateDonationRequest(
    Guid MemberId,
    Guid? DonationAccountId,
    DonationType Type,
    DonationMethod Method,
    string DonationDate,
    decimal Amount,
    string? IdempotencyKey,
    string? EnteredBy,
    string? ServiceName,
    string? Notes,
    Guid? ObligationId = null
);

public record CreateDonationResponse(
    Guid DonationId,
    int Version,
    bool IsDuplicate
);

public record DonationLedgerItem(
    Guid Id,
    Guid MemberId,
    Guid? DonationAccountId,
    DonationType Type,
    DonationMethod Method,
    string DonationDate,
    decimal Amount,
    DonationStatus Status,
    string? ServiceName,
    string? Notes,
    DateTime CreatedAtUtc,
    string CreatedBy,
    DateTime? VoidedAtUtc,
    string? VoidedBy,
    string? VoidReason,
    int Version
);

public record DonationLedgerResponse(
    int Page,
    int PageSize,
    int TotalCount,
    ImmutableArray<DonationLedgerItem> Donations
);

public record VoidDonationRequest(
    string Reason,
    string? EnteredBy,
    int ExpectedVersion
);
