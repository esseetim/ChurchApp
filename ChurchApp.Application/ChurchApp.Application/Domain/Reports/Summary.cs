using ChurchApp.Application.Domain.Members;

namespace ChurchApp.Application.Domain.Reports;

public class Summary
{
    public Guid Id { get; set; }
    public SummaryType Type { get; set; }
    public SummaryPeriodType PeriodType { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    public string? ServiceName { get; set; }
    public Guid? MemberId { get; set; }
    public Member? Member { get; set; }

    public decimal TotalAmount { get; set; }
    public int DonationCount { get; set; }
    public required string BreakdownJson { get; set; }
    public DateTime GeneratedAtUtc { get; set; }
}
