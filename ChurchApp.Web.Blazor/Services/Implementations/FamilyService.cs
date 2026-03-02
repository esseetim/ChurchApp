using System.Net.Http.Json;
using System.Text.Json;
using ChurchApp.Web.Blazor.Models;
using ChurchApp.Web.Blazor.Serialization;

namespace ChurchApp.Web.Blazor.Services.Implementations;

public class FamilyService(IHttpClientFactory httpClientFactory) : IFamilyService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        TypeInfoResolver = ChurchAppJsonContext.Default
    };

    public async Task<FamiliesResponse> GetFamiliesAsync(
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
        return await httpClient.GetFromJsonAsync<FamiliesResponse>(
            $"/api/families?{queryString}", 
            JsonOptions, 
            cancellationToken)
            ?? throw new InvalidOperationException("Failed to deserialize response");
    }

    public async Task<CreateFamilyResponse> CreateFamilyAsync(
        CreateFamilyRequest request, 
        CancellationToken cancellationToken = default)
    {
        using var httpClient = httpClientFactory.CreateClient("ChurchAppApi");
        var response = await httpClient.PostAsJsonAsync(
            "/api/families", 
            request, 
            JsonOptions, 
            cancellationToken);
        
        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadFromJsonAsync<CreateFamilyResponse>(JsonOptions, cancellationToken) 
            ?? throw new InvalidOperationException("Failed to deserialize response");
    }

    public async Task AddFamilyMemberAsync(
        Guid familyId, 
        AddFamilyMemberRequest request, 
        CancellationToken cancellationToken = default)
    {
        using var httpClient = httpClientFactory.CreateClient("ChurchAppApi");
        var response = await httpClient.PostAsJsonAsync(
            $"/api/families/{familyId}/members", 
            request, 
            JsonOptions, 
            cancellationToken);
        
        response.EnsureSuccessStatusCode();
    }
}
