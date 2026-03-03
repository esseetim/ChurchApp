using ChurchApp.Application.Domain.Obligations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChurchApp.Application.Infrastructure.Configurations.Obligations;

/// <summary>
/// Configures the FinancialObligation entity for EF Core.
/// </summary>
public class FinancialObligationConfiguration : IEntityTypeConfiguration<FinancialObligation>
{
    public void Configure(EntityTypeBuilder<FinancialObligation> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Type).HasConversion<int>();
        builder.Property(x => x.Status).HasConversion<int>();
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.TotalAmount).HasPrecision(18, 2);

        // Configure relationship to Member
        builder.HasOne(x => x.Member)
            .WithMany(x => x.FinancialObligations)
            .HasForeignKey(x => x.MemberId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure relationship to Payments (Donations)
        builder.HasMany(x => x.Payments)
            .WithOne(x => x.Obligation)
            .HasForeignKey(x => x.ObligationId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes for query performance
        builder.HasIndex(x => new { x.MemberId, x.Status });
        builder.HasIndex(x => x.DueDate);

        // Check constraint: TotalAmount must be positive
        builder.ToTable(t => 
            t.HasCheckConstraint("CK_FinancialObligations_TotalAmount_Positive", "\"TotalAmount\" > 0"));
    }
}
