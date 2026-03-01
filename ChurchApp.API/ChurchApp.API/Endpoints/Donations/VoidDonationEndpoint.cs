using ChurchApp.API.Endpoints.Contracts;
using ChurchApp.Application.Domain.Donations;
using ChurchApp.Application.Infrastructure;
using ChurchApp.Application.Infrastructure.Transactions;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.API.Endpoints.Donations;

public sealed class VoidDonationEndpoint(ChurchAppDbContext dbContext, IUnitOfWork unitOfWork)
    : Endpoint<VoidDonationRequest, VoidDonationResponse>
{
    public override void Configure()
    {
        Post("/api/donations/{donationId:guid}/void");
        AllowAnonymous();
    }

    public override async Task HandleAsync(VoidDonationRequest req, CancellationToken ct)
    {
        var donationIdRaw = Route<string>("donationId");
        if (!Guid.TryParse(donationIdRaw, out var donationId))
        {
            AddError("Invalid donation id.");
            await SendErrorsAsync(cancellation: ct);
            return;
        }

        if (string.IsNullOrWhiteSpace(req.Reason))
        {
            AddError("Void reason is required.");
            await SendErrorsAsync(cancellation: ct);
            return;
        }

        var donation = await dbContext.Donations.SingleOrDefaultAsync(x => x.Id == donationId, ct);
        if (donation is null)
        {
            AddError("Donation not found.");
            await SendErrorsAsync(cancellation: ct);
            return;
        }

        if (donation.Status == DonationStatus.Voided)
        {
            AddError("Donation is already voided.");
            await SendErrorsAsync(cancellation: ct);
            return;
        }

        if (donation.Version != req.ExpectedVersion)
        {
            AddError("Donation has been modified by another operation.");
            await SendErrorsAsync(cancellation: ct);
            return;
        }

        donation.Void(req.Reason.Trim(), req.EnteredBy);

        await unitOfWork.ExecuteInTransactionAsync(async txCt =>
        {
            dbContext.DonationAudits.Add(new DonationAudit
            {
                Id = Guid.NewGuid(),
                DonationId = donation.Id,
                Action = DonationAuditAction.Voided,
                OccurredAtUtc = DateTime.UtcNow,
                PerformedBy = string.IsNullOrWhiteSpace(req.EnteredBy) ? "system" : req.EnteredBy.Trim(),
                Reason = donation.VoidReason,
                SnapshotJson = $"{{\"donationId\":\"{donation.Id}\",\"status\":\"{donation.Status}\",\"version\":{donation.Version}}}"
            });

            await unitOfWork.SaveChangesAsync(txCt);
        }, ct);

        await SendAsync(new VoidDonationResponse(donation.Id, donation.Version, donation.VoidedAtUtc!.Value), cancellation: ct);
    }
}
