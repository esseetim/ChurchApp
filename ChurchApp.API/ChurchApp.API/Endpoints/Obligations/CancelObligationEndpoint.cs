using ChurchApp.Application.Infrastructure;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.API.Endpoints.Obligations;

/// <summary>
/// Cancels a financial obligation.
/// </summary>
public sealed class CancelObligationEndpoint(ChurchAppDbContext dbContext)
    : EndpointWithoutRequest
{
    public override void Configure()
    {
        Delete("/api/members/{memberId}/obligations/{id}");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Cancel obligation";
            s.Description = "Cancels a financial obligation. The obligation is not deleted but marked as cancelled.";
            s.Response(204, "Obligation cancelled");
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
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

        // Cancel using domain method
        obligation.Cancel();

        await dbContext.SaveChangesAsync(ct);

        await SendNoContentAsync(ct);
    }
}
