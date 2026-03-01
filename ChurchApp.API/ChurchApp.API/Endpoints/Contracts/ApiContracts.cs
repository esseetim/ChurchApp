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
    string? ServiceName,
    string? Notes);

public sealed record CreateDonationResponse(Guid DonationId);

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

public sealed record DonationTypeBreakdownDto(DonationType Type, decimal TotalAmount, int DonationCount);

public sealed record TimeRangeReportResponse(
    DateOnly StartDate,
    DateOnly EndDate,
    decimal TotalAmount,
    int DonationCount,
    List<DonationTypeBreakdownDto> Breakdown);
