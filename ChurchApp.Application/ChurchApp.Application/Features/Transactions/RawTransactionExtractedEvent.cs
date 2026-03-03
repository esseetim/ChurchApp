namespace ChurchApp.Application.Features.Transactions;

/// <summary>
/// Integration event raised when a new raw transaction is extracted from email.
/// Triggers the resolution workflow.
/// </summary>
public sealed record RawTransactionExtractedEvent(
    Guid RawTransactionId,
    string ProviderTransactionId) : IIntegrationEvent;
