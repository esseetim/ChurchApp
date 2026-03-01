using System.Text.Json;
using ChurchApp.API;
using ChurchApp.API.Endpoints.Contracts;
using ChurchApp.Application.Domain.Reports;
using ChurchApp.Application.Infrastructure;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.API.Endpoints.Summaries;

public sealed class GetServiceSummariesEndpoint(ChurchAppDbContext dbContext)
    : Endpoint<GetServiceSummariesRequest, SummariesResponse>
{
    public override void Configure()
    {
        Get("/api/summaries/service");
        AllowAnonymous();
    }

    public override async Task HandleAsync(GetServiceSummariesRequest req, CancellationToken ct)
    {
        var summaries = await dbContext.Summaries
            .Where(x => x.Type == SummaryType.Service
                        && x.ServiceName == req.ServiceName
                        && x.StartDate >= req.StartDate
                        && x.EndDate <= req.EndDate)
            .OrderByDescending(x => x.GeneratedAtUtc)
            .Select(x => new SummaryItemDto(
                x.Id,
                x.Type,
                x.PeriodType,
                x.StartDate,
                x.EndDate,
                x.ServiceName,
                x.MemberId,
                x.FamilyId,
                x.TotalAmount,
                x.DonationCount,
                x.GeneratedAtUtc))
            .ToListAsync(ct);

        var response = new SummariesResponse(summaries);

        if (req.PersistReport)
        {
            dbContext.Reports.Add(new Report
            {
                Id = Guid.NewGuid(),
                Type = ReportType.Service,
                GeneratedAtUtc = DateTime.UtcNow,
                ServiceName = req.ServiceName,
                StartDate = req.StartDate,
                EndDate = req.EndDate,
                ParametersJson = JsonSerializer.Serialize(req, AppJsonSerializerContext.Default.GetServiceSummariesRequest),
                OutputJson = JsonSerializer.Serialize(response, AppJsonSerializerContext.Default.SummariesResponse)
            });

            await dbContext.SaveChangesAsync(ct);
        }

        await SendAsync(response, cancellation: ct);
    }
}
