using System.Text.Json;
using ChurchApp.API;
using ChurchApp.API.Endpoints.Contracts;
using ChurchApp.Application.Domain.Reports;
using ChurchApp.Application.Infrastructure;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.API.Endpoints.Reports;

public sealed class GetTimeRangeReportEndpoint(ChurchAppDbContext dbContext)
    : Endpoint<TimeRangeReportRequest, TimeRangeReportResponse>
{
    public override void Configure()
    {
        Get("/api/reports/time-range");
        AllowAnonymous();
    }

    public override async Task HandleAsync(TimeRangeReportRequest req, CancellationToken ct)
    {
        var donations = dbContext.Donations
            .Where(x => x.Status == DonationStatus.Active
                        && x.DonationDate >= req.StartDate
                        && x.DonationDate <= req.EndDate);

        var totalAmount = await donations
            .Select(x => (decimal?)x.Amount)
            .SumAsync(ct) ?? 0m;
        var donationCount = await donations.CountAsync(ct);

        var breakdownRows = await donations
            .GroupBy(x => x.Type)
            .Select(x => new
            {
                Type = x.Key,
                TotalAmount = x.Sum(v => v.Amount),
                DonationCount = x.Count()
            })
            .OrderBy(x => x.Type)
            .ToListAsync(ct);
        
        var breakdown = breakdownRows
            .Select(x => new DonationTypeBreakdownDto(x.Type, x.TotalAmount, x.DonationCount))
            .ToList();

        var response = new TimeRangeReportResponse(
            req.StartDate,
            req.EndDate,
            totalAmount,
            donationCount,
            breakdown);

        if (req.PersistReport)
        {
            dbContext.Reports.Add(new Report
            {
                Id = Guid.NewGuid(),
                Type = ReportType.TimeRange,
                GeneratedAtUtc = DateTime.UtcNow,
                StartDate = req.StartDate,
                EndDate = req.EndDate,
                ParametersJson = JsonSerializer.Serialize(req, AppJsonSerializerContext.Default.TimeRangeReportRequest),
                OutputJson = JsonSerializer.Serialize(response, AppJsonSerializerContext.Default.TimeRangeReportResponse)
            });

            await dbContext.SaveChangesAsync(ct);
        }

        await SendAsync(response, cancellation: ct);
    }
}
