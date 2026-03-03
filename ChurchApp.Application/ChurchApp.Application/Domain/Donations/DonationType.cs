namespace ChurchApp.Application.Domain.Donations;

/// <summary>
/// Represents the type of donation or payment being made.
/// </summary>
public enum DonationType
{
    /// <summary>
    /// A general offering donation.
    /// </summary>
    GeneralOffering = 1,

    /// <summary>
    /// A tithe payment.
    /// </summary>
    Tithe = 2,

    /// <summary>
    /// A contribution to the building fund.
    /// </summary>
    BuildingFund = 3,

    /// <summary>
    /// A payment toward a fundraising pledge.
    /// </summary>
    PledgePayment = 4,

    /// <summary>
    /// A payment toward club or ministry dues.
    /// </summary>
    ClubDuePayment = 5
}
