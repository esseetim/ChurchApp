namespace ChurchApp.Application.Domain.Donations;

public class DonationAudit
{
    public Guid Id { get; set; }
    public Guid DonationId { get; set; }
    public Donation Donation { get; set; } = null!;

    public DonationAuditAction Action { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public required string PerformedBy { get; set; }
    public string? Reason { get; set; }
    public required string SnapshotJson { get; set; }
}
