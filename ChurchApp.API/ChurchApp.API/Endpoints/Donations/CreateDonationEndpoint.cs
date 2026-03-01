using ChurchApp.API.Endpoints.Contracts;
using ChurchApp.Application.Domain.Donations;
using ChurchApp.Application.Infrastructure;
using ChurchApp.Application.Infrastructure.Transactions;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.API.Endpoints.Donations;

public sealed class CreateDonationEndpoint(ChurchAppDbContext dbContext, IUnitOfWork unitOfWork)
    : Endpoint<CreateDonationRequest, CreateDonationResponse>
{
    public override void Configure()
    {
        Post("/api/donations");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Create donation";
            s.Description = "Creates a donation and upserts applicable summaries in the same transaction.";
            s.Response<CreateDonationResponse>(201, "Donation created");
        });
    }

    public override async Task HandleAsync(CreateDonationRequest req, CancellationToken ct)
    {
        if (req.Amount == 0)
        {
            AddError("Amount must be non-zero.");
            await SendErrorsAsync(cancellation: ct);
            return;
        }

        var memberExists = await dbContext.Members.AnyAsync(x => x.Id == req.MemberId, ct);
        if (!memberExists)
        {
            AddError("Member not found.");
            await SendErrorsAsync(cancellation: ct);
            return;
        }

        if (req.DonationAccountId.HasValue)
        {
            var accountExists = await dbContext.DonationAccounts.AnyAsync(
                x => x.Id == req.DonationAccountId.Value && x.MemberId == req.MemberId,
                ct);

            if (!accountExists)
            {
                AddError("Donation account not found for member.");
                await SendErrorsAsync(cancellation: ct);
                return;
            }
        }

        var donation = Donation.Create(
            req.MemberId,
            req.DonationAccountId,
            req.Type,
            req.Method,
            req.DonationDate,
            req.Amount,
            req.ServiceName,
            req.Notes);

        await unitOfWork.ExecuteInTransactionAsync(async txCt =>
        {
            dbContext.Donations.Add(donation);
            await unitOfWork.SaveChangesAsync(txCt);
        }, ct);

        await SendAsync(new CreateDonationResponse(donation.Id), 201, ct);
    }
}
