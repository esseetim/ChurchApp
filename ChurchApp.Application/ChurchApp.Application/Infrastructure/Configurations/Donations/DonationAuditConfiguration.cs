using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChurchApp.Application.Infrastructure.Configurations.Donations;

public class DonationAuditConfiguration : IEntityTypeConfiguration<DonationAudit>
{
    public void Configure(EntityTypeBuilder<DonationAudit> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Action).HasConversion<int>();
        builder.Property(x => x.PerformedBy).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(500);
        builder.Property(x => x.SnapshotJson).IsRequired();

        builder.HasOne(x => x.Donation)
            .WithMany(x => x.Audits)
            .HasForeignKey(x => x.DonationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.DonationId, x.OccurredAtUtc });
    }
}
