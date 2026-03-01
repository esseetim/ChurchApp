using ChurchApp.API.Endpoints.Contracts;
using ChurchApp.Application.Domain.Families;
using ChurchApp.Application.Infrastructure;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.API.Endpoints.Families;

public sealed class CreateFamilyEndpoint(ChurchAppDbContext dbContext)
    : Endpoint<CreateFamilyRequest, CreateFamilyResponse>
{
    public override void Configure()
    {
        Post("/api/families");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CreateFamilyRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
        {
            AddError("Family name is required.");
            await SendErrorsAsync(cancellation: ct);
            return;
        }

        var name = req.Name.Trim();
        var exists = await dbContext.Families.AnyAsync(x => x.Name == name, ct);
        if (exists)
        {
            AddError("A family with this name already exists.");
            await SendErrorsAsync(cancellation: ct);
            return;
        }

        var family = new Family
        {
            Id = Guid.NewGuid(),
            Name = name
        };

        dbContext.Families.Add(family);
        await dbContext.SaveChangesAsync(ct);

        await SendAsync(new CreateFamilyResponse(family.Id), 201, ct);
    }
}
