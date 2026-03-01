using ChurchApp.Application.Domain.Families;
using ChurchApp.Application.Domain.Members;

namespace ChurchApp.Application.Domain.Reports;

public class Report
{
    public Guid Id { get; set; }
    public ReportType Type { get; set; }
    public DateTime GeneratedAtUtc { get; set; }

    public string? ServiceName { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }

    public Guid? MemberId { get; set; }
    public Member? Member { get; set; }

    public Guid? FamilyId { get; set; }
    public Family? Family { get; set; }

    public required string ParametersJson { get; set; }
    public required string OutputJson { get; set; }
}
