using ChurchApp.API.Endpoints.Contracts;
using ChurchApp.Application.Infrastructure;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.API.Endpoints.Families;

public sealed class UpdateFamilyEndpoint(ChurchAppDbContext dbContext)
    : Endpoint<UpdateFamilyRequest, EmptyResponse>
{
    public override void Configure()
    {
        Put("/api/families/{id:guid}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(UpdateFamilyRequest req, CancellationToken ct)
    {
        var idRaw = Route<string>("id");
        if (!Guid.TryParse(idRaw, out var familyId))
        {
            AddError("Invalid family id.");
            await SendErrorsAsync(cancellation: ct);
            return;
        }

        if (string.IsNullOrWhiteSpace(req.Name))
        {
            AddError("Family name is required.");
            await SendErrorsAsync(cancellation: ct);
            return;
        }

        var family = await dbContext.Families.SingleOrDefaultAsync(x => x.Id == familyId, ct);
        if (family is null)
        {
            AddError("Family not found.");
            await SendErrorsAsync(cancellation: ct);
            return;
        }

        var normalizedName = req.Name.Trim();
        var exists = await dbContext.Families.AnyAsync(x => x.Id != familyId && x.Name == normalizedName, ct);
        if (exists)
        {
            AddError("A family with this name already exists.");
            await SendErrorsAsync(cancellation: ct);
            return;
        }

        family.Name = normalizedName;
        await dbContext.SaveChangesAsync(ct);
        await SendNoContentAsync(ct);
    }
}
