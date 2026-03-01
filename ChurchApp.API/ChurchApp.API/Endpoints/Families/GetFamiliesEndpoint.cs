using ChurchApp.API.Endpoints.Contracts;
using ChurchApp.Application.Infrastructure;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.API.Endpoints.Families;

public sealed class GetFamiliesEndpoint(ChurchAppDbContext dbContext)
    : Endpoint<GetFamiliesRequest, FamiliesResponse>
{
    public override void Configure()
    {
        Get("/api/families");
        AllowAnonymous();
    }

    public override async Task HandleAsync(GetFamiliesRequest req, CancellationToken ct)
    {
        var page = req.Page < 1 ? 1 : req.Page;
        var pageSize = req.PageSize is < 1 or > 250 ? 50 : req.PageSize;

        var query = dbContext.Families.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var search = req.Search.Trim();
            query = query.Where(x => EF.Functions.ILike(x.Name, $"%{search}%"));
        }

        var totalCount = await query.CountAsync(ct);
        var families = await query
            .OrderBy(x => x.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new FamilyListItemDto(
                x.Id,
                x.Name,
                x.Members.Count))
            .ToListAsync(ct);

        await SendAsync(new FamiliesResponse(page, pageSize, totalCount, families), cancellation: ct);
    }
}
