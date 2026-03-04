// Example: Using DonationAmount in FastEndpoints API

using ChurchApp.Primitives.Donations;
using FastEndpoints;

namespace ChurchApp.API.Examples;

/// <summary>
/// Example 1: Query String Binding
/// GET /api/donations/search?minAmount=50.00&maxAmount=1000.00
/// </summary>
public sealed class SearchDonationsEndpoint : Endpoint<SearchDonationsRequest>
{
    public override void Configure()
    {
        Get("/api/donations/search");
        AllowAnonymous();
    }

    public override async Task HandleAsync(SearchDonationsRequest req, CancellationToken ct)
    {
        // TypeConverter automatically parses query string parameters
        // minAmount and maxAmount are strongly-typed DonationAmount instances
        
        var results = await SearchDonationsAsync(req.MinAmount, req.MaxAmount, ct);
        await SendOkAsync(results, ct);
    }
    
    private Task<List<DonationDto>> SearchDonationsAsync(
        DonationAmount min, 
        DonationAmount max, 
        CancellationToken ct)
    {
        // Business logic here - min and max are guaranteed valid
        // (TypeConverter threw exception if invalid, ModelState caught it)
        return Task.FromResult(new List<DonationDto>());
    }
}

public sealed record SearchDonationsRequest
{
    // TypeConverter kicks in here for query string binding
    public DonationAmount MinAmount { get; init; } = DonationAmount.One;
    public DonationAmount MaxAmount { get; init; } = DonationAmount.Hundred;
}

/// <summary>
/// Example 2: Route Parameter Binding
/// GET /api/donations/by-amount/{amount}
/// </summary>
public sealed class GetDonationsByAmountEndpoint : EndpointWithoutRequest
{
    public override void Configure()
    {
        Get("/api/donations/by-amount/{amount}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        // TypeConverter parses route parameter
        // Route("{amount}") automatically converted to DonationAmount
        var amount = Route<DonationAmount>("amount");
        
        var donations = await FindDonationsByAmountAsync(amount, ct);
        await SendOkAsync(donations, ct);
    }
    
    private Task<List<DonationDto>> FindDonationsByAmountAsync(
        DonationAmount amount, 
        CancellationToken ct)
    {
        return Task.FromResult(new List<DonationDto>());
    }
}

/// <summary>
/// Example 3: Request Body with JSON
/// POST /api/donations
/// Body: { "amount": 50.00, "memberId": "guid", "notes": "Weekly tithe" }
/// </summary>
public sealed class CreateDonationEndpoint : Endpoint<CreateDonationRequest>
{
    public override void Configure()
    {
        Post("/api/donations");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CreateDonationRequest req, CancellationToken ct)
    {
        // DonationAmountJsonConverter handles JSON deserialization
        // TypeConverter not used here, but both converters use ErrorOr internally
        
        // Amount is guaranteed valid because:
        // 1. JsonConverter validated during deserialization
        // 2. If invalid, 400 Bad Request returned automatically
        
        var donationId = await CreateDonationAsync(req, ct);
        await SendCreatedAtAsync<GetDonationEndpoint>(
            new { id = donationId }, 
            donationId, 
            ct);
    }
    
    private Task<Guid> CreateDonationAsync(CreateDonationRequest req, CancellationToken ct)
    {
        // req.Amount is strongly-typed, validated DonationAmount
        return Task.FromResult(Guid.NewGuid());
    }
}

public sealed record CreateDonationRequest
{
    public DonationAmount Amount { get; init; }
    public Guid MemberId { get; init; }
    public string? Notes { get; init; }
}

/// <summary>
/// Example 4: Validation Integration
/// POST /api/donations/validate
/// </summary>
public sealed class ValidatedDonationEndpoint : Endpoint<ValidatedDonationRequest>
{
    public override void Configure()
    {
        Post("/api/donations/validate");
        AllowAnonymous();
    }

    public override async Task HandleAsync(ValidatedDonationRequest req, CancellationToken ct)
    {
        // FastEndpoints + FluentValidation integration
        // Validation runs AFTER TypeConverter parses the value
        
        await SendOkAsync(new { message = "Donation is valid!", amount = req.Amount }, ct);
    }
}

public sealed record ValidatedDonationRequest
{
    public DonationAmount Amount { get; init; }
    public Guid MemberId { get; init; }
}

public sealed class ValidatedDonationRequestValidator : Validator<ValidatedDonationRequest>
{
    public ValidatedDonationRequestValidator()
    {
        // Validation on the DonationAmount itself
        RuleFor(x => x.Amount)
            .Must(amount => (decimal)amount >= DonationAmount.Minimum)
            .WithMessage("Amount must be at least $1.00");
        
        RuleFor(x => x.Amount)
            .Must(amount => (decimal)amount <= 1_000_000m)
            .WithMessage("Amount cannot exceed $1,000,000");
        
        // Can also validate business rules
        RuleFor(x => x.Amount)
            .Must((req, amount) => IsReasonableForMember(req.MemberId, amount))
            .WithMessage("Amount seems unusually high for this member");
    }
    
    private bool IsReasonableForMember(Guid memberId, DonationAmount amount)
    {
        // Custom business logic
        return true;
    }
}

public sealed record DonationDto(Guid Id, DonationAmount Amount, string MemberName);

public sealed class GetDonationEndpoint : EndpointWithoutRequest
{
    public override void Configure()
    {
        Get("/api/donations/{id}");
    }

    public override Task HandleAsync(CancellationToken ct)
    {
        return Task.CompletedTask;
    }
}

/*
 * Testing the API:
 * 
 * 1. Query String:
 *    curl "http://localhost:5000/api/donations/search?minAmount=50.00&maxAmount=1000"
 *    
 * 2. Route Parameter:
 *    curl "http://localhost:5000/api/donations/by-amount/75.50"
 *    
 * 3. JSON Body:
 *    curl -X POST http://localhost:5000/api/donations \
 *      -H "Content-Type: application/json" \
 *      -d '{"amount": 100.00, "memberId": "3fa85f64-5717-4562-b3fc-2c963f66afa6", "notes": "Tithe"}'
 *    
 * 4. Invalid Amount (negative):
 *    curl "http://localhost:5000/api/donations/search?minAmount=-50.00"
 *    Response: 400 Bad Request with ModelState error
 *    
 * 5. Invalid Format:
 *    curl "http://localhost:5000/api/donations/search?minAmount=abc"
 *    Response: 400 Bad Request with format error
 */
