using System.Collections.Immutable;

namespace ChurchApp.Web.Blazor.Models;

public record SummaryItem(
    Guid Id,
    int Type,
    SummaryPeriodType PeriodType,
    string StartDate,
    string EndDate,
    string? ServiceName,
    Guid? MemberId,
    Guid? FamilyId,
    decimal TotalAmount,
    int DonationCount,
    DateTime GeneratedAtUtc
);

public record SummariesResponse(
    ImmutableArray<SummaryItem> Summaries
);

public record DonationTypeBreakdown(
    DonationType Type,
    decimal TotalAmount,
    int DonationCount
);

public record TimeRangeReportResponse(
    string StartDate,
    string EndDate,
    decimal TotalAmount,
    int DonationCount,
    ImmutableArray<DonationTypeBreakdown> Breakdown
);
