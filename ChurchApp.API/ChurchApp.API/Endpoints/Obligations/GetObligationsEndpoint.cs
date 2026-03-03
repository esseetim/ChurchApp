using ChurchApp.API.Endpoints.Contracts;
using ChurchApp.Application.Infrastructure;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.API.Endpoints.Obligations;

/// <summary>
/// Gets all financial obligations for a member.
/// </summary>
public sealed class GetObligationsEndpoint(ChurchAppDbContext dbContext)
    : EndpointWithoutRequest<ObligationsResponse>
{
    public override void Configure()
    {
        Get("/api/members/{memberId}/obligations");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get member obligations";
            s.Description = "Retrieves all financial obligations for a member with calculated payment progress.";
            s.Response<ObligationsResponse>(200, "List of obligations retrieved");
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var memberId = Route<Guid>("memberId");

        // Load obligations with their payments for calculation
        var obligations = await dbContext.FinancialObligations
            .Where(x => x.MemberId == memberId)
            .Include(x => x.Payments.Where(p => p.Status == Application.Domain.Donations.DonationStatus.Active))
            .OrderByDescending(x => x.Status == Application.Domain.Obligations.ObligationStatus.Active)
            .ThenBy(x => x.DueDate)
            .Select(x => new ObligationDto(
                x.Id,
                x.MemberId,
                x.Type,
                x.Title,
                x.TotalAmount,
                x.AmountPaid,
                x.BalanceRemaining,
                x.StartDate,
                x.DueDate,
                x.Status))
            .ToListAsync(ct);

        await SendAsync(new ObligationsResponse(obligations), cancellation: ct);
    }
}
