using System.Net.Http.Json;
using System.Text.Json;
using ChurchApp.Web.Blazor.Models;
using ChurchApp.Web.Blazor.Serialization;

namespace ChurchApp.Web.Blazor.Services.Implementations;

public class MemberService(IHttpClientFactory httpClientFactory) : IMemberService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        TypeInfoResolver = ChurchAppJsonContext.Default
    };

    public async Task<MembersResponse> GetMembersAsync(
        string? search = null, 
        int page = 1, 
        int pageSize = 200, 
        CancellationToken cancellationToken = default)
    {
        var queryParams = new List<string>
        {
            $"page={page}",
            $"pageSize={pageSize}"
        };
        
        if (!string.IsNullOrEmpty(search)) 
            queryParams.Add($"search={Uri.EscapeDataString(search)}");
        
        var queryString = string.Join("&", queryParams);
        
        using var httpClient = httpClientFactory.CreateClient("ChurchAppApi");
        return await httpClient.GetFromJsonAsync<MembersResponse>(
            $"/api/members?{queryString}", 
            JsonOptions, 
            cancellationToken)
            ?? throw new InvalidOperationException("Failed to deserialize response");
    }

    public async Task<CreateMemberResponse> CreateMemberAsync(
        CreateMemberRequest request, 
        CancellationToken cancellationToken = default)
    {
        using var httpClient = httpClientFactory.CreateClient("ChurchAppApi");
        var response = await httpClient.PostAsJsonAsync(
            "/api/members", 
            request, 
            JsonOptions, 
            cancellationToken);
        
        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadFromJsonAsync<CreateMemberResponse>(JsonOptions, cancellationToken) 
            ?? throw new InvalidOperationException("Failed to deserialize response");
    }

    public async Task UpdateMemberAsync(
        Guid memberId,
        UpdateMemberRequest request,
        CancellationToken cancellationToken = default)
    {
        using var httpClient = httpClientFactory.CreateClient("ChurchAppApi");
        var response = await httpClient.PutAsJsonAsync(
            $"/api/members/{memberId}",
            request,
            JsonOptions,
            cancellationToken);

        response.EnsureSuccessStatusCode();
    }

    public async Task<MemberDonationAccountsResponse> GetDonationAccountsAsync(
        Guid memberId,
        CancellationToken cancellationToken = default)
    {
        using var httpClient = httpClientFactory.CreateClient("ChurchAppApi");
        return await httpClient.GetFromJsonAsync<MemberDonationAccountsResponse>(
                   $"/api/members/{memberId}/accounts",
                   JsonOptions,
                   cancellationToken)
               ?? throw new InvalidOperationException("Failed to deserialize response");
    }

    public async Task<DonationAccount> CreateDonationAccountAsync(
        Guid memberId,
        CreateDonationAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        using var httpClient = httpClientFactory.CreateClient("ChurchAppApi");
        var response = await httpClient.PostAsJsonAsync(
            $"/api/members/{memberId}/accounts",
            request,
            JsonOptions,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<DonationAccount>(JsonOptions, cancellationToken)
               ?? throw new InvalidOperationException("Failed to deserialize response");
    }

    public async Task<DonationAccount> UpdateDonationAccountAsync(
        Guid memberId,
        Guid accountId,
        UpdateDonationAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        using var httpClient = httpClientFactory.CreateClient("ChurchAppApi");
        var response = await httpClient.PutAsJsonAsync(
            $"/api/members/{memberId}/accounts/{accountId}",
            request,
            JsonOptions,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<DonationAccount>(JsonOptions, cancellationToken)
               ?? throw new InvalidOperationException("Failed to deserialize response");
    }

    public async Task DeleteDonationAccountAsync(
        Guid memberId,
        Guid accountId,
        CancellationToken cancellationToken = default)
    {
        using var httpClient = httpClientFactory.CreateClient("ChurchAppApi");
        using var response = await httpClient.DeleteAsync($"/api/members/{memberId}/accounts/{accountId}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<MemberFamiliesResponse> GetMemberFamiliesAsync(Guid memberId, CancellationToken cancellationToken = default)
    {
        using var httpClient = httpClientFactory.CreateClient("ChurchAppApi");
        return await httpClient.GetFromJsonAsync<MemberFamiliesResponse>(
                   $"/api/members/{memberId}/families",
                   JsonOptions,
                   cancellationToken)
               ?? throw new InvalidOperationException("Failed to deserialize response");
    }
}
