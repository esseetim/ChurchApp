using ChurchApp.API.Endpoints.Contracts;
using ChurchApp.Application.Domain.Donations;
using ChurchApp.Application.Domain.Reports;
using ChurchApp.Application.Infrastructure;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.API.Endpoints.Summaries;

public sealed class GetSummaryDonationsEndpoint(ChurchAppDbContext dbContext)
    : Endpoint<SummaryDonationsRequest, SummaryDonationsResponse>
{
    public override void Configure()
    {
        Get("/api/summaries/donations");
        AllowAnonymous();
    }

    public override async Task HandleAsync(SummaryDonationsRequest req, CancellationToken ct)
    {
        var page = req.Page < 1 ? 1 : req.Page;
        var pageSize = req.PageSize is < 1 or > 250 ? 50 : req.PageSize;

        var summary = await dbContext.Summaries.SingleOrDefaultAsync(x => x.Id == req.SummaryId, ct);
        if (summary is null)
        {
            AddError("Summary not found.");
            await SendErrorsAsync(cancellation: ct);
            return;
        }

        var query = dbContext.Donations
            .Where(x => x.Status == DonationStatus.Active
                        && x.DonationDate >= summary.StartDate
                        && x.DonationDate <= summary.EndDate);

        if (summary.Type == SummaryType.Service && !string.IsNullOrWhiteSpace(summary.ServiceName))
        {
            query = query.Where(x => x.ServiceName == summary.ServiceName);
        }

        if (summary.Type == SummaryType.Member && summary.MemberId.HasValue)
        {
            query = query.Where(x => x.MemberId == summary.MemberId.Value);
        }

        if (summary.Type == SummaryType.Family && summary.FamilyId.HasValue)
        {
            var memberIds = dbContext.FamilyMembers
                .Where(x => x.FamilyId == summary.FamilyId.Value)
                .Select(x => x.MemberId);

            query = query.Where(x => memberIds.Contains(x.MemberId));
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(x => x.DonationDate)
            .ThenByDescending(x => x.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new DonationLedgerItemDto(
                x.Id,
                x.MemberId,
                x.DonationAccountId,
                x.ObligationId,
                x.Type,
                x.Method,
                x.DonationDate,
                x.Amount,
                x.Status,
                x.ServiceName,
                x.Notes,
                x.CreatedAtUtc,
                x.CreatedBy,
                x.VoidedAtUtc,
                x.VoidedBy,
                x.VoidReason,
                x.Version))
            .ToListAsync(ct);

        await SendAsync(new SummaryDonationsResponse(req.SummaryId, page, pageSize, totalCount, items), cancellation: ct);
    }
}
