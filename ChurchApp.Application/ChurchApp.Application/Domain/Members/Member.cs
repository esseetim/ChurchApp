using ChurchApp.Application.Domain.Obligations;

namespace ChurchApp.Application.Domain.Members;

public class Member
{
    public Guid Id { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public EmailAddress? Email { get; set; }
    public PhoneNumber? PhoneNumber { get; set; }

    public ICollection<FamilyMember> FamilyMembers { get; set; } = [];
    public ICollection<DonationAccount> DonationAccounts { get; set; } = [];
    public ICollection<Donation> Donations { get; set; } = [];
    public ICollection<FinancialObligation> FinancialObligations { get; set; } = [];
}
