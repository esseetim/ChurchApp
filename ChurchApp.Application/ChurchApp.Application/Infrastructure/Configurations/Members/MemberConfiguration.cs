using ChurchApp.Application.Domain.Members;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChurchApp.Application.Infrastructure.Configurations.Members;

public class MemberConfiguration : IEntityTypeConfiguration<Member>
{
    public void Configure(EntityTypeBuilder<Member> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FirstName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.LastName).HasMaxLength(150).IsRequired();
        
        // Value converter for EmailAddress - using explicit ValueConverter class
        builder.Property(x => x.Email)
            .HasConversion(
                v => v.HasValue ? (string?)v.Value : null,
                v => v == null ? (EmailAddress?)null : EmailAddress.Create(v).Value,
                new ValueComparer<EmailAddress?>(
                    (l, r) => (l == null && r == null) || (l != null && r != null && l.Value.Equals(r.Value)),
                    v => v == null ? 0 : v.Value.GetHashCode(),
                    v => v))
            .HasMaxLength(EmailAddress.MaxLength)
            .IsRequired(false);
        
        // Value converter for PhoneNumber - using explicit ValueConverter class
        builder.Property(x => x.PhoneNumber)
            .HasConversion(
                v => v.HasValue ? (string?)v.Value : null,
                v => v == null ? (PhoneNumber?)null : PhoneNumber.Create(v).Value,
                new ValueComparer<PhoneNumber?>(
                    (l, r) => (l == null && r == null) || (l != null && r != null && l.Value.Equals(r.Value)),
                    v => v == null ? 0 : v.Value.GetHashCode(),
                    v => v))
            .HasMaxLength(PhoneNumber.MaxLength)
            .IsRequired(false);
    }
}
