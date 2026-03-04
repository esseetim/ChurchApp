using ChurchApp.Primitives.Obligations;

namespace ChurchApp.Web.Blazor.Models;

/// <summary>
/// Financial obligation data transfer object.
/// </summary>
public record ObligationDto(
    Guid Id,
    Guid MemberId,
    ObligationType Type,
    string Title,
    decimal TotalAmount,
    decimal AmountPaid,
    decimal BalanceRemaining,
    DateOnly StartDate,
    DateOnly DueDate,
    ObligationStatus Status
);

/// <summary>
/// Response containing a list of obligations.
/// </summary>
public record ObligationsResponse(
    IReadOnlyList<ObligationDto> Obligations
);

/// <summary>
/// Request to create a new obligation.
/// </summary>
public record CreateObligationRequest(
    ObligationType Type,
    string Title,
    decimal TotalAmount,
    DateOnly StartDate,
    DateOnly DueDate
);
