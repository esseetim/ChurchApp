using ChurchApp.Application.Domain.Donations;
using ChurchApp.Application.Domain.Families;

namespace ChurchApp.Application.Domain.Members;

public class Member
{
    public Guid Id { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }

    public ICollection<FamilyMember> FamilyMembers { get; set; } = new List<FamilyMember>();
    public ICollection<DonationAccount> DonationAccounts { get; set; } = new List<DonationAccount>();
    public ICollection<Donation> Donations { get; set; } = new List<Donation>();
}
