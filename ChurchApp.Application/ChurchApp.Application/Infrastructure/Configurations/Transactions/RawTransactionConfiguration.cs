using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChurchApp.Application.Infrastructure.Configurations.Transactions;

/// <summary>
/// EF Core configuration for RawTransaction entity.
/// Implements idempotency guarantees at the database level.
/// </summary>
public sealed class RawTransactionConfiguration : IEntityTypeConfiguration<RawTransaction>
{
    public void Configure(EntityTypeBuilder<RawTransaction> builder)
    {
        builder.ToTable("RawTransactions");
        
        builder.HasKey(x => x.Id);
        
        // Critical: Unique index on ProviderTransactionId for idempotency
        // This prevents duplicate processing if the same email is processed twice
        builder.HasIndex(x => x.ProviderTransactionId)
            .IsUnique()
            .HasDatabaseName("IX_RawTransactions_ProviderTransactionId_Unique");
        
        // Index for querying unmatched transactions in the UI
        builder.HasIndex(x => x.Status)
            .HasDatabaseName("IX_RawTransactions_Status");
        
        // Composite index for finding transactions by provider and status
        builder.HasIndex(x => new { x.Provider, x.Status })
            .HasDatabaseName("IX_RawTransactions_Provider_Status");

        // Required fields
        builder.Property(x => x.ProviderTransactionId)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(x => x.GmailMessageId)
            .HasMaxLength(100);
        
        builder.Property(x => x.SenderName)
            .IsRequired()
            .HasMaxLength(200);
        
        builder.Property(x => x.SenderHandle)
            .HasMaxLength(100);
        
        // Decimal configuration with proper precision
        builder.Property(x => x.Amount)
            .HasPrecision(18, 2)
            .IsRequired();
        
        builder.Property(x => x.TransactionDate)
            .IsRequired();
        
        builder.Property(x => x.Memo)
            .HasMaxLength(500);
        
        // Raw content stored as JSON text
        builder.Property(x => x.RawContentJson)
            .IsRequired()
            .HasColumnType("text");
        
        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<int>();
        
        builder.Property(x => x.Provider)
            .IsRequired()
            .HasConversion<int>();
        
        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        // Relationship to Donation (optional FK)
        builder.HasOne(x => x.ResolvedDonation)
            .WithMany()
            .HasForeignKey(x => x.ResolvedDonationId)
            .OnDelete(DeleteBehavior.SetNull);

        // Domain events are not persisted
        builder.Ignore(x => x.DomainEvents);
    }
}
