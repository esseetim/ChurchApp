using ChurchApp.Application.Domain.Common;

namespace ChurchApp.Application.Domain.Donations;

public sealed record DonationVoidedDomainEvent(
    Guid DonationId,
    Guid MemberId,
    DateOnly DonationDate,
    decimal Amount,
    string? ServiceName) : IDomainEvent;
