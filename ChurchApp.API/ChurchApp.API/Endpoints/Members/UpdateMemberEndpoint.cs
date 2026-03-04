using ChurchApp.API.Endpoints.Contracts;
using ChurchApp.Application.Infrastructure;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.API.Endpoints.Members;

public sealed class UpdateMemberEndpoint(ChurchAppDbContext dbContext)
    : Endpoint<UpdateMemberRequest, EmptyResponse>
{
    public override void Configure()
    {
        Put("/api/members/{id:guid}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(UpdateMemberRequest req, CancellationToken ct)
    {
        var idRaw = Route<string>("id");
        if (!Guid.TryParse(idRaw, out var memberId))
        {
            AddError("Invalid member id.");
            await SendErrorsAsync(cancellation: ct);
            return;
        }

        if (string.IsNullOrWhiteSpace(req.FirstName) || string.IsNullOrWhiteSpace(req.LastName))
        {
            AddError("FirstName and LastName are required.");
            await SendErrorsAsync(cancellation: ct);
            return;
        }

        var member = await dbContext.Members.SingleOrDefaultAsync(x => x.Id == memberId, ct);
        if (member is null)
        {
            AddError("Member not found.");
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
            
            // Check if email already exists for another member
            var emailExists = await dbContext.Members.AnyAsync(
                x => x.Id != memberId && x.Email.HasValue && x.Email == emailAddress, 
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

        member.FirstName = req.FirstName.Trim();
        member.LastName = req.LastName.Trim();
        member.Email = emailAddress;
        member.PhoneNumber = phoneNumber;
        await dbContext.SaveChangesAsync(ct);
        await SendNoContentAsync(ct);
    }
}
