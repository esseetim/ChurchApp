using ChurchApp.Application.Domain.Donations;
using ChurchApp.Application.Domain.Reports;

namespace ChurchApp.API.Endpoints.Contracts;

public sealed record CreateDonationRequest(
    Guid MemberId,
    Guid? DonationAccountId,
    DonationType Type,
    DonationMethod Method,
    DateOnly DonationDate,
    decimal Amount,
    string? IdempotencyKey,
    string? EnteredBy,
    string? ServiceName,
    string? Notes);

public sealed record CreateDonationResponse(Guid DonationId, int Version, bool IsDuplicate);

public sealed record VoidDonationRequest(string Reason, string? EnteredBy, int ExpectedVersion);

public sealed record VoidDonationResponse(Guid DonationId, int Version, DateTime VoidedAtUtc);

public sealed record GetDonationsRequest(
    int Page = 1,
    int PageSize = 50,
    DateOnly? StartDate = null,
    DateOnly? EndDate = null,
    Guid? MemberId = null,
    Guid? FamilyId = null,
    DonationType? Type = null,
    DonationMethod? Method = null,
    bool IncludeVoided = false);

public sealed record DonationLedgerItemDto(
    Guid Id,
    Guid MemberId,
    Guid? DonationAccountId,
    DonationType Type,
    DonationMethod Method,
    DateOnly DonationDate,
    decimal Amount,
    DonationStatus Status,
    string? ServiceName,
    string? Notes,
    DateTime CreatedAtUtc,
    string CreatedBy,
    DateTime? VoidedAtUtc,
    string? VoidedBy,
    string? VoidReason,
    int Version);

public sealed record DonationLedgerResponse(int Page, int PageSize, int TotalCount, List<DonationLedgerItemDto> Donations);

public sealed record GetServiceSummariesRequest(string ServiceName, DateOnly StartDate, DateOnly EndDate, bool PersistReport = false);

public sealed record GetMemberSummariesRequest(Guid MemberId, SummaryPeriodType? PeriodType, DateOnly? StartDate, DateOnly? EndDate, bool PersistReport = false);

public sealed record GetFamilySummariesRequest(Guid FamilyId, SummaryPeriodType? PeriodType, DateOnly? StartDate, DateOnly? EndDate, bool PersistReport = false);

public sealed record TimeRangeReportRequest(DateOnly StartDate, DateOnly EndDate, bool PersistReport = false);

public sealed record SummaryItemDto(
    Guid Id,
    SummaryType Type,
    SummaryPeriodType PeriodType,
    DateOnly StartDate,
    DateOnly EndDate,
    string? ServiceName,
    Guid? MemberId,
    Guid? FamilyId,
    decimal TotalAmount,
    int DonationCount,
    DateTime GeneratedAtUtc);

public sealed record SummariesResponse(List<SummaryItemDto> Summaries);

public sealed record SummaryDonationsRequest(Guid SummaryId, int Page = 1, int PageSize = 50);

public sealed record SummaryDonationsResponse(Guid SummaryId, int Page, int PageSize, int TotalCount, List<DonationLedgerItemDto> Donations);

public sealed record DonationTypeBreakdownDto(DonationType Type, decimal TotalAmount, int DonationCount);

public sealed record TimeRangeReportResponse(
    DateOnly StartDate,
    DateOnly EndDate,
    decimal TotalAmount,
    int DonationCount,
    List<DonationTypeBreakdownDto> Breakdown);
