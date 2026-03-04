namespace ChurchApp.Primitives.Obligations;

/// <summary>
/// Represents the current status of a financial obligation.
/// </summary>
public enum ObligationStatus
{
    /// <summary>
    /// The obligation is active and awaiting payment.
    /// </summary>
    Active = 1,

    /// <summary>
    /// The obligation has been fully paid.
    /// </summary>
    Fulfilled = 2,

    /// <summary>
    /// The obligation has been cancelled and is no longer expected.
    /// </summary>
    Cancelled = 3
}
