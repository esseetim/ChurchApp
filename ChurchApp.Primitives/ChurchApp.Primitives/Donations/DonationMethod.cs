using System.Collections.Frozen;

namespace ChurchApp.Primitives.Donations;

/// <summary>
/// Represents the payment method used for a donation.
/// </summary>
public enum DonationMethod : byte
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
    /// Another payment method.
    /// </summary>
    Other = 6
}

public static class DonationMethodExtensions
{
    public static readonly FrozenSet<DonationMethod> AllMethods = [
        DonationMethod.Cash, DonationMethod.CashApp, DonationMethod.Zelle, DonationMethod.Check, DonationMethod.Card, 
        DonationMethod.Other];
    
    public static readonly FrozenSet<string> AllMethodNames = [
        nameof(DonationMethod.Cash), nameof(DonationMethod.CashApp), nameof(DonationMethod.Zelle), 
        nameof(DonationMethod.Check), nameof(DonationMethod.Card), nameof(DonationMethod.Other)];
}