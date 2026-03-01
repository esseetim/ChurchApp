using ChurchApp.API.Endpoints.Contracts;
using ChurchApp.Application.Infrastructure;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.API.Endpoints.Members;

public sealed class GetMembersEndpoint(ChurchAppDbContext dbContext)
    : Endpoint<GetMembersRequest, MembersResponse>
{
    public override void Configure()
    {
        Get("/api/members");
        AllowAnonymous();
    }

    public override async Task HandleAsync(GetMembersRequest req, CancellationToken ct)
    {
        var page = req.Page < 1 ? 1 : req.Page;
        var pageSize = req.PageSize is < 1 or > 250 ? 50 : req.PageSize;

        var query = dbContext.Members.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var search = req.Search.Trim();
            query = query.Where(x =>
                EF.Functions.ILike(x.FirstName, $"%{search}%")
                || EF.Functions.ILike(x.LastName, $"%{search}%")
                || (x.Email != null && EF.Functions.ILike(x.Email, $"%{search}%"))
                || (x.PhoneNumber != null && EF.Functions.ILike(x.PhoneNumber, $"%{search}%")));
        }

        var totalCount = await query.CountAsync(ct);
        var members = await query
            .OrderBy(x => x.LastName)
            .ThenBy(x => x.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new MemberListItemDto(
                x.Id,
                x.FirstName,
                x.LastName,
                x.Email,
                x.PhoneNumber))
            .ToListAsync(ct);

        await SendAsync(new MembersResponse(page, pageSize, totalCount, members), cancellation: ct);
    }
}
