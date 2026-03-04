namespace ChurchApp.Primitives.Donations;

/// <summary>
/// Represents the payment method used for a donation.
/// </summary>
public enum DonationMethod
{
    /// <summary>
    /// Cash payment.
    /// </summary>
    Cash = 1,

    /// <summary>
    /// CashApp (Square) payment.
    /// </summary>
    CashApp = 2,

    /// <summary>
    /// Zelle payment.
    /// </summary>
    Zelle = 3,

    /// <summary>
    /// Check payment.
    /// </summary>
    Check = 4,

    /// <summary>
    /// Credit/debit card payment.
    /// </summary>
    Card = 5,

    /// <summary>
    /// Other payment method.
    /// </summary>
    Other = 6
}
