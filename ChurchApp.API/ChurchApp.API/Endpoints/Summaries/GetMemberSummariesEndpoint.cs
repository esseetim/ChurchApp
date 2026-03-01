using System.Text.Json;
using ChurchApp.API;
using ChurchApp.API.Endpoints.Contracts;
using ChurchApp.Application.Domain.Reports;
using ChurchApp.Application.Infrastructure;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.API.Endpoints.Summaries;

public sealed class GetMemberSummariesEndpoint(ChurchAppDbContext dbContext)
    : Endpoint<GetMemberSummariesRequest, SummariesResponse>
{
    public override void Configure()
    {
        Get("/api/summaries/member");
        AllowAnonymous();
    }

    public override async Task HandleAsync(GetMemberSummariesRequest req, CancellationToken ct)
    {
        var query = dbContext.Summaries
            .Where(x => x.Type == SummaryType.Member && x.MemberId == req.MemberId);

        if (req.PeriodType is not null)
        {
            query = query.Where(x => x.PeriodType == req.PeriodType.Value);
        }

        if (req.StartDate is not null)
        {
            query = query.Where(x => x.StartDate >= req.StartDate.Value);
        }

        if (req.EndDate is not null)
        {
            query = query.Where(x => x.EndDate <= req.EndDate.Value);
        }

        var summaries = await query
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
                Type = ReportType.Member,
                GeneratedAtUtc = DateTime.UtcNow,
                MemberId = req.MemberId,
                StartDate = req.StartDate,
                EndDate = req.EndDate,
                ParametersJson = JsonSerializer.Serialize(req, AppJsonSerializerContext.Default.GetMemberSummariesRequest),
                OutputJson = JsonSerializer.Serialize(response, AppJsonSerializerContext.Default.SummariesResponse)
            });

            await dbContext.SaveChangesAsync(ct);
        }

        await SendAsync(response, cancellation: ct);
    }
}
