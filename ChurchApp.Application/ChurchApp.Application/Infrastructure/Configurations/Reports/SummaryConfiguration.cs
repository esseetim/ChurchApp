using ChurchApp.Application.Domain.Reports;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChurchApp.Application.Infrastructure.Configurations.Reports;

public class SummaryConfiguration : IEntityTypeConfiguration<Summary>
{
    public void Configure(EntityTypeBuilder<Summary> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Type).HasConversion<int>();
        builder.Property(x => x.PeriodType).HasConversion<int>();
        builder.Property(x => x.ServiceName).HasMaxLength(200);
        builder.Property(x => x.TotalAmount).HasPrecision(18, 2);
        builder.Property(x => x.BreakdownJson).IsRequired();
        builder.ToTable(t => t.HasCheckConstraint("CK_Summaries_TotalAmount_NotNegative", "\"TotalAmount\" >= 0"));

        builder.HasOne(x => x.Member)
            .WithMany()
            .HasForeignKey(x => x.MemberId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => new { x.Type, x.PeriodType, x.StartDate, x.EndDate, x.GeneratedAtUtc });
    }
}
