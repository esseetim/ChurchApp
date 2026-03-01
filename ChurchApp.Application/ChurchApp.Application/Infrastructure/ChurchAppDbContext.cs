using System.Diagnostics.CodeAnalysis;
using ChurchApp.Application.Domain.Donations;
using ChurchApp.Application.Domain.Families;
using ChurchApp.Application.Domain.Members;
using ChurchApp.Application.Domain.Reports;
using ChurchApp.Application.Infrastructure.Configurations.Donations;
using ChurchApp.Application.Infrastructure.Configurations.Families;
using ChurchApp.Application.Infrastructure.Configurations.Members;
using ChurchApp.Application.Infrastructure.Configurations.Reports;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.Application.Infrastructure;

[method: RequiresUnreferencedCode("EF Core isn't fully compatible with trimming.")]
[method: RequiresDynamicCode("EF Core isn't fully compatible with NativeAOT.")]
public sealed class ChurchAppDbContext(DbContextOptions<ChurchAppDbContext> options) : DbContext(options)
{
    public DbSet<Member> Members => Set<Member>();
    public DbSet<Family> Families => Set<Family>();
    public DbSet<FamilyMember> FamilyMembers => Set<FamilyMember>();
    public DbSet<DonationAccount> DonationAccounts => Set<DonationAccount>();
    public DbSet<Donation> Donations => Set<Donation>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<Summary> Summaries => Set<Summary>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new MemberConfiguration());
        modelBuilder.ApplyConfiguration(new FamilyConfiguration());
        modelBuilder.ApplyConfiguration(new FamilyMemberConfiguration());
        modelBuilder.ApplyConfiguration(new DonationAccountConfiguration());
        modelBuilder.ApplyConfiguration(new DonationConfiguration());
        modelBuilder.ApplyConfiguration(new ReportConfiguration());
        modelBuilder.ApplyConfiguration(new SummaryConfiguration());

        base.OnModelCreating(modelBuilder);
    }
}
