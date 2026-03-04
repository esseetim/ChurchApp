using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChurchApp.Application.Infrastructure.Configurations.Donations;

public class DonationAccountConfiguration : IEntityTypeConfiguration<DonationAccount>
{
    public void Configure(EntityTypeBuilder<DonationAccount> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Method).HasConversion<int>();
        
        // Value converter for PaymentHandle
        // Note: Validation uses "Other" method since we don't have method context during deserialization
        builder.Property(x => x.Handle)
            .HasConversion(
                handle => (string)handle,
                value => PaymentHandle.Create(value, DonationMethod.Other).Value)
            .HasMaxLength(PaymentHandle.MaxLength)
            .IsRequired();
        
        builder.Property(x => x.DisplayName).HasMaxLength(200);

        builder.HasOne(x => x.Member)
            .WithMany(x => x.DonationAccounts)
            .HasForeignKey(x => x.MemberId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.Method, x.Handle }).IsUnique();
    }
}
