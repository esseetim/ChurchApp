using ChurchApp.Application.Infrastructure;
using ChurchApp.Application.Infrastructure.DomainEvents;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.Application.Features.Obligations;

/// <summary>
/// Handles automatic fulfillment of financial obligations when donations are made.
/// </summary>
public sealed class ObligationFulfillmentHandler(ChurchAppDbContext dbContext)
    : IDomainEventHandler<DonationCreatedDomainEvent>
{
    public async Task HandleAsync(DonationCreatedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        // Only process if donation is linked to an obligation
        if (!domainEvent.ObligationId.HasValue)
        {
            return;
        }

        // Load the obligation with its active payments
        var obligation = await dbContext.FinancialObligations
            .Include(x => x.Payments.Where(p => p.Status == DonationStatus.Active))
            .Where(x => x.Id == domainEvent.ObligationId.Value)
            .SingleOrDefaultAsync(cancellationToken);

        // Check if obligation should be fulfilled
        obligation?.CheckFulfillment();

        // SaveChanges will be called by the parent transaction/unit of work
    }
}
