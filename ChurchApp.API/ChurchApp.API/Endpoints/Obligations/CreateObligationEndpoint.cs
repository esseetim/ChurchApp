using ChurchApp.API.Endpoints.Contracts;
using ChurchApp.Application.Domain.Obligations;
using ChurchApp.Application.Infrastructure;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.API.Endpoints.Obligations;

/// <summary>
/// Creates a financial obligation for a member.
/// </summary>
public sealed class CreateObligationEndpoint(ChurchAppDbContext dbContext)
    : Endpoint<CreateObligationRequest, CreateObligationResponse>
{
    public override void Configure()
    {
        Post("/api/members/{memberId}/obligations");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Create obligation";
            s.Description = "Creates a financial obligation (pledge or due) for a member.";
            s.Response<CreateObligationResponse>(201, "Obligation created");
        });
    }

    public override async Task HandleAsync(CreateObligationRequest req, CancellationToken ct)
    {
        var memberId = Route<Guid>("memberId");

        // Validate member exists
        var memberExists = await dbContext.Members.AnyAsync(x => x.Id == memberId, ct);
        if (!memberExists)
        {
            AddError("Member not found.");
            await SendErrorsAsync(cancellation: ct);
            return;
        }

        // Create obligation using domain factory
        FinancialObligation obligation;
        try
        {
            obligation = FinancialObligation.Create(
                memberId,
                req.Type,
                req.Title,
                req.TotalAmount,
                req.StartDate,
                req.DueDate);
        }
        catch (ArgumentException ex)
        {
            AddError(ex.Message);
            await SendErrorsAsync(cancellation: ct);
            return;
        }

        dbContext.FinancialObligations.Add(obligation);
        await dbContext.SaveChangesAsync(ct);

        await SendAsync(new CreateObligationResponse(obligation.Id), 201, ct);
    }
}
