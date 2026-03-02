using System.Net.Http.Json;
using System.Text.Json;
using ChurchApp.Web.Blazor.Models;
using ChurchApp.Web.Blazor.Serialization;

namespace ChurchApp.Web.Blazor.Services.Implementations;

/// <summary>
/// DonationService using IHttpClientFactory with source-generated JSON serialization.
/// Follows modern .NET best practices for performance and resilience.
/// </summary>
public class DonationService(IHttpClientFactory httpClientFactory) : IDonationService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        TypeInfoResolver = ChurchAppJsonContext.Default
    };

    public async Task<CreateDonationResponse> CreateDonationAsync(
        CreateDonationRequest request, 
        CancellationToken cancellationToken = default)
    {
        using var httpClient = httpClientFactory.CreateClient("ChurchAppApi");
        var response = await httpClient.PostAsJsonAsync(
            "/api/donations", 
            request, 
            JsonOptions, 
            cancellationToken);
        
        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadFromJsonAsync<CreateDonationResponse>(JsonOptions, cancellationToken) 
            ?? throw new InvalidOperationException("Failed to deserialize response");
    }

    public async Task<DonationLedgerResponse> GetDonationsAsync(
        int page, 
        int pageSize, 
        string? startDate = null, 
        string? endDate = null, 
        Guid? memberId = null, 
        Guid? familyId = null, 
        DonationType? type = null, 
        DonationMethod? method = null, 
        bool includeVoided = false, 
        CancellationToken cancellationToken = default)
    {
        var queryParams = new List<string>
        {
            $"page={page}",
            $"pageSize={pageSize}"
        };
        
        if (!string.IsNullOrEmpty(startDate)) queryParams.Add($"startDate={Uri.EscapeDataString(startDate)}");
        if (!string.IsNullOrEmpty(endDate)) queryParams.Add($"endDate={Uri.EscapeDataString(endDate)}");
        if (memberId.HasValue) queryParams.Add($"memberId={memberId.Value}");
        if (familyId.HasValue) queryParams.Add($"familyId={familyId.Value}");
        if (type.HasValue) queryParams.Add($"type={type.Value}");
        if (method.HasValue) queryParams.Add($"method={method.Value}");
        if (includeVoided) queryParams.Add("includeVoided=true");
        
        var queryString = string.Join("&", queryParams);
        
        using var httpClient = httpClientFactory.CreateClient("ChurchAppApi");
        return await httpClient.GetFromJsonAsync<DonationLedgerResponse>(
            $"/api/donations?{queryString}", 
            JsonOptions, 
            cancellationToken)
            ?? throw new InvalidOperationException("Failed to deserialize response");
    }

    public async Task VoidDonationAsync(
        Guid donationId, 
        VoidDonationRequest request, 
        CancellationToken cancellationToken = default)
    {
        using var httpClient = httpClientFactory.CreateClient("ChurchAppApi");
        var response = await httpClient.PostAsJsonAsync(
            $"/api/donations/{donationId}/void", 
            request, 
            JsonOptions, 
            cancellationToken);
        
        response.EnsureSuccessStatusCode();
    }
}
