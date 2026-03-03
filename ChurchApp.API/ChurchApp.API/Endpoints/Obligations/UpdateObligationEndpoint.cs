using ChurchApp.API.Endpoints.Contracts;
using ChurchApp.Application.Infrastructure;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.API.Endpoints.Obligations;

/// <summary>
/// Updates an existing financial obligation.
/// </summary>
public sealed class UpdateObligationEndpoint(ChurchAppDbContext dbContext)
    : Endpoint<UpdateObligationRequest>
{
    public override void Configure()
    {
        Put("/api/members/{memberId}/obligations/{id}");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Update obligation";
            s.Description = "Updates the title, total amount, or due date of a financial obligation.";
            s.Response(204, "Obligation updated");
        });
    }

    public override async Task HandleAsync(UpdateObligationRequest req, CancellationToken ct)
    {
        var memberId = Route<Guid>("memberId");
        var obligationId = Route<Guid>("id");

        var obligation = await dbContext.FinancialObligations
            .Where(x => x.Id == obligationId && x.MemberId == memberId)
            .SingleOrDefaultAsync(ct);

        if (obligation is null)
        {
            AddError("Obligation not found for member.");
            await SendErrorsAsync(cancellation: ct);
            return;
        }

        // Update using domain method
        try
        {
            obligation.Update(req.Title, req.TotalAmount, req.DueDate);
        }
        catch (ArgumentException ex)
        {
            AddError(ex.Message);
            await SendErrorsAsync(cancellation: ct);
            return;
        }

        await dbContext.SaveChangesAsync(ct);

        await SendNoContentAsync(ct);
    }
}
