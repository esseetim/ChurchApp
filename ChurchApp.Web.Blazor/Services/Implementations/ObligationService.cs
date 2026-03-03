using System.Net.Http.Json;
using ChurchApp.Web.Blazor.Models;

namespace ChurchApp.Web.Blazor.Services.Implementations;

/// <summary>
/// Implementation of obligation service using HttpClient.
/// </summary>
public sealed class ObligationService : IObligationService
{
    private readonly HttpClient _httpClient;

    public ObligationService(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<ObligationsResponse> GetMemberObligationsAsync(
        Guid memberId,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetFromJsonAsync<ObligationsResponse>(
            $"api/members/{memberId}/obligations",
            cancellationToken);

        return response ?? new ObligationsResponse(Array.Empty<ObligationDto>());
    }

    public async Task<Guid> CreateObligationAsync(
        Guid memberId,
        CreateObligationRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"api/members/{memberId}/obligations",
            request,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<CreateObligationResponse>(
            cancellationToken: cancellationToken);

        return result?.ObligationId ?? throw new InvalidOperationException("Failed to create obligation");
    }
}

public record CreateObligationResponse(Guid ObligationId);
