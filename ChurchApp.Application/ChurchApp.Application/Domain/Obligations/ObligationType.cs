namespace ChurchApp.Application.Domain.Obligations;

/// <summary>
/// Represents the type of financial obligation committed by a member.
/// </summary>
public enum ObligationType
{
    /// <summary>
    /// A pledge made during a fundraising campaign.
    /// </summary>
    FundraisingPledge = 1,

    /// <summary>
    /// Membership dues for a church club or ministry.
    /// </summary>
    Dues = 2
}
