using ChurchApp.API.Endpoints.Contracts;
using ChurchApp.Application.Domain.Donations;
using ChurchApp.Application.Infrastructure;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.API.Endpoints.Donations;

public sealed class GetDonationsEndpoint(ChurchAppDbContext dbContext)
    : Endpoint<GetDonationsRequest, DonationLedgerResponse>
{
    public override void Configure()
    {
        Get("/api/donations");
        AllowAnonymous();
    }

    public override async Task HandleAsync(GetDonationsRequest req, CancellationToken ct)
    {
        var page = req.Page < 1 ? 1 : req.Page;
        var pageSize = req.PageSize is < 1 or > 250 ? 50 : req.PageSize;

        var query = dbContext.Donations.Where(_ => true);

        if (!req.IncludeVoided)
        {
            query = query.Where(x => x.Status == DonationStatus.Active);
        }

        if (req.StartDate is not null)
        {
            query = query.Where(x => x.DonationDate >= req.StartDate.Value);
        }

        if (req.EndDate is not null)
        {
            query = query.Where(x => x.DonationDate <= req.EndDate.Value);
        }

        if (req.MemberId is not null)
        {
            query = query.Where(x => x.MemberId == req.MemberId.Value);
        }

        if (req.FamilyId is not null)
        {
            var memberIds = dbContext.FamilyMembers
                .Where(x => x.FamilyId == req.FamilyId.Value)
                .Select(x => x.MemberId);

            query = query.Where(x => memberIds.Contains(x.MemberId));
        }

        if (req.Type is not null)
        {
            query = query.Where(x => x.Type == req.Type.Value);
        }

        if (req.Method is not null)
        {
            query = query.Where(x => x.Method == req.Method.Value);
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

        await SendAsync(new DonationLedgerResponse(page, pageSize, totalCount, items), cancellation: ct);
    }
}
