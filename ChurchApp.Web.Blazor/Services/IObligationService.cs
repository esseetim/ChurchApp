using ChurchApp.Web.Blazor.Models;

namespace ChurchApp.Web.Blazor.Services;

/// <summary>
/// Service for managing financial obligations (pledges and dues).
/// </summary>
public interface IObligationService
{
    /// <summary>
    /// Gets all obligations for a specific member.
    /// </summary>
    Task<ObligationsResponse> GetMemberObligationsAsync(
        Guid memberId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new obligation for a member.
    /// </summary>
    Task<Guid> CreateObligationAsync(
        Guid memberId,
        CreateObligationRequest request,
        CancellationToken cancellationToken = default);
}
