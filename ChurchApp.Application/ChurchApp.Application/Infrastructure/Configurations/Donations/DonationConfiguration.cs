using ChurchApp.Application.Domain.Donations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChurchApp.Application.Infrastructure.Configurations.Donations;

public class DonationConfiguration : IEntityTypeConfiguration<Donation>
{
    public void Configure(EntityTypeBuilder<Donation> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Type).HasConversion<int>();
        builder.Property(x => x.Method).HasConversion<int>();
        builder.Property(x => x.Status).HasConversion<int>().HasDefaultValue(DonationStatus.Active);
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.IdempotencyKey).HasMaxLength(100);
        builder.Property(x => x.ServiceName).HasMaxLength(200);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.CreatedBy).HasMaxLength(120).IsRequired();
        builder.Property(x => x.VoidedBy).HasMaxLength(120);
        builder.Property(x => x.VoidReason).HasMaxLength(500);
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.ToTable(t => t.HasCheckConstraint("CK_Donations_Amount_NotZero", "\"Amount\" <> 0"));

        builder.HasOne(x => x.Member)
            .WithMany(x => x.Donations)
            .HasForeignKey(x => x.MemberId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.DonationAccount)
            .WithMany(x => x.Donations)
            .HasForeignKey(x => x.DonationAccountId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => new { x.DonationDate, x.Type });
        builder.HasIndex(x => x.IdempotencyKey).IsUnique();
    }
}
