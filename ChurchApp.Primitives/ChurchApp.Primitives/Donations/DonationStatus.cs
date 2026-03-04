namespace ChurchApp.Primitives.Donations;

/// <summary>
/// Represents the status of a donation record.
/// </summary>
public enum DonationStatus : byte
{
    /// <summary>
    /// Sentinel value for EF Core. Should not be used in business logic.
    /// </summary>
    Unspecified = 0,

    /// <summary>
    /// The donation is active and counted in financial reports.
    /// </summary>
    Active = 1,

    /// <summary>
    /// The donation has been voided and should be excluded from reports.
    /// </summary>
    Voided = 2
}
