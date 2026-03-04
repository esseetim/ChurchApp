using ChurchApp.API.Endpoints.Contracts;
using ChurchApp.Application.Domain.Donations;
using ChurchApp.Application.Domain.Members;
using ChurchApp.Application.Infrastructure;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.API.Endpoints.Members;

public sealed class CreateMemberEndpoint(ChurchAppDbContext dbContext)
    : Endpoint<CreateMemberRequest, CreateMemberResponse>
{
    public override void Configure()
    {
        Post("/api/members");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CreateMemberRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.FirstName) || string.IsNullOrWhiteSpace(req.LastName))
        {
            AddError("FirstName and LastName are required.");
            await SendErrorsAsync(cancellation: ct);
            return;
        }

        // Validate and create EmailAddress if provided
        EmailAddress? emailAddress = null;
        if (!string.IsNullOrWhiteSpace(req.Email))
        {
            var emailResult = EmailAddress.Create(req.Email);
            if (emailResult.IsError)
            {
                AddError($"Invalid email: {emailResult.FirstError.Description}");
                await SendErrorsAsync(cancellation: ct);
                return;
            }
            
            emailAddress = emailResult.Value;
            
            // Check if email already exists
            var emailExists = await dbContext.Members.AnyAsync(
                x => x.Email.HasValue && x.Email == emailAddress, 
                ct);
            
            if (emailExists)
            {
                AddError("A member with this email already exists.");
                await SendErrorsAsync(cancellation: ct);
                return;
            }
        }

        // Validate and create PhoneNumber if provided
        PhoneNumber? phoneNumber = null;
        if (!string.IsNullOrWhiteSpace(req.PhoneNumber))
        {
            var phoneResult = PhoneNumber.Create(req.PhoneNumber);
            if (phoneResult.IsError)
            {
                AddError($"Invalid phone number: {phoneResult.FirstError.Description}");
                await SendErrorsAsync(cancellation: ct);
                return;
            }
            
            phoneNumber = phoneResult.Value;
        }

        var member = new Member
        {
            Id = Guid.NewGuid(),
            FirstName = req.FirstName.Trim(),
            LastName = req.LastName.Trim(),
            Email = emailAddress,
            PhoneNumber = phoneNumber
        };

        var donationAccounts = new List<DonationAccount>();
        if (req.DonationAccounts is not null)
        {
            foreach (var accountRequest in req.DonationAccounts)
            {
                if (string.IsNullOrWhiteSpace(accountRequest.Handle))
                {
                    AddError("Donation account handle is required.");
                    await SendErrorsAsync(cancellation: ct);
                    return;
                }

                if (accountRequest.Method == DonationMethod.Cash)
                {
                    AddError("Cash is not a valid donation account method.");
                    await SendErrorsAsync(cancellation: ct);
                    return;
                }

                // Validate and create PaymentHandle
                var handleResult = PaymentHandle.Create(accountRequest.Handle, accountRequest.Method);
                if (handleResult.IsError)
                {
                    AddError($"Invalid payment handle: {handleResult.FirstError.Description}");
                    await SendErrorsAsync(cancellation: ct);
                    return;
                }

                var handle = handleResult.Value;
                
                // Check if handle already exists for this method
                var methodHandleExists = await dbContext.DonationAccounts.AnyAsync(
                    x => x.Method == accountRequest.Method && x.Handle == handle,
                    ct);

                if (methodHandleExists)
                {
                    AddError($"Donation account already exists for method '{accountRequest.Method}' and handle '{(string)handle}'.");
                    await SendErrorsAsync(cancellation: ct);
                    return;
                }

                donationAccounts.Add(new DonationAccount
                {
                    Id = Guid.NewGuid(),
                    MemberId = member.Id,
                    Method = accountRequest.Method,
                    Handle = handle,
                    DisplayName = string.IsNullOrWhiteSpace(accountRequest.DisplayName) ? null : accountRequest.DisplayName.Trim(),
                    IsActive = true
                });
            }
        }

        dbContext.Members.Add(member);
        if (donationAccounts.Count > 0)
        {
            dbContext.DonationAccounts.AddRange(donationAccounts);
        }
        await dbContext.SaveChangesAsync(ct);

        await SendAsync(new CreateMemberResponse(member.Id), 201, ct);
    }
}
