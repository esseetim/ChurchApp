using ChurchApp.Application.Domain.Members;

namespace ChurchApp.Application.Domain.Donations;

public class Donation
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public Member Member { get; set; } = null!;

    public Guid? DonationAccountId { get; set; }
    public DonationAccount? DonationAccount { get; set; }

    public DonationType Type { get; set; }
    public DonationMethod Method { get; set; }
    public DateOnly DonationDate { get; set; }
    public decimal Amount { get; set; }
    public string? Notes { get; set; }
}
