using ChurchApp.Application.Domain.Common;
using ChurchApp.Application.Domain.Members;

namespace ChurchApp.Application.Domain.Donations;

public class Donation : IHasDomainEvents
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
    public DonationStatus Status { get; set; }
    public string? IdempotencyKey { get; set; }
    public string? ServiceName { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public required string CreatedBy { get; set; }
    public DateTime? VoidedAtUtc { get; set; }
    public string? VoidedBy { get; set; }
    public string? VoidReason { get; set; }
    public int Version { get; set; }

    public ICollection<DonationAudit> Audits { get; set; } = new List<DonationAudit>();

    public List<IDomainEvent> DomainEvents { get; } = [];

    public static Donation Create(
        Guid memberId,
        Guid? donationAccountId,
        DonationType type,
        DonationMethod method,
        DateOnly donationDate,
        decimal amount,
        string? idempotencyKey,
        string? enteredBy,
        string? serviceName,
        string? notes)
    {
        var actor = string.IsNullOrWhiteSpace(enteredBy) ? "system" : enteredBy.Trim();

        var donation = new Donation
        {
            Id = Guid.NewGuid(),
            MemberId = memberId,
            DonationAccountId = donationAccountId,
            Type = type,
            Method = method,
            DonationDate = donationDate,
            Amount = amount,
            Status = DonationStatus.Active,
            IdempotencyKey = string.IsNullOrWhiteSpace(idempotencyKey) ? null : idempotencyKey.Trim(),
            ServiceName = serviceName,
            Notes = notes,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedBy = actor
        };

        donation.DomainEvents.Add(new DonationCreatedDomainEvent(
            donation.Id,
            donation.MemberId,
            donation.DonationDate,
            donation.Amount,
            donation.ServiceName));

        return donation;
    }

    public void Void(string reason, string? enteredBy)
    {
        if (Status == DonationStatus.Voided)
        {
            return;
        }

        Status = DonationStatus.Voided;
        VoidedAtUtc = DateTime.UtcNow;
        VoidedBy = string.IsNullOrWhiteSpace(enteredBy) ? "system" : enteredBy.Trim();
        VoidReason = reason;
        Version += 1;

        DomainEvents.Add(new DonationVoidedDomainEvent(
            Id,
            MemberId,
            DonationDate,
            Amount,
            ServiceName));
    }
}
