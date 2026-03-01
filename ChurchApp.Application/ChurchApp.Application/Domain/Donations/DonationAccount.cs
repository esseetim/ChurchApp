using ChurchApp.Application.Domain.Members;

namespace ChurchApp.Application.Domain.Donations;

public class DonationAccount
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public Member Member { get; set; } = null!;

    public DonationMethod Method { get; set; }
    public required string Handle { get; set; }
    public string? DisplayName { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Donation> Donations { get; set; } = [];
}
