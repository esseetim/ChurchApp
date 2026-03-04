using System.Text;
using System.Text.Json;
using ChurchApp.API;
using ChurchApp.API.Endpoints.Contracts;
using ChurchApp.Application.Domain.Donations;
using ChurchApp.Application.Domain.Reports;
using ChurchApp.Application.Infrastructure;
using ChurchApp.Primitives.Donations;
using Microsoft.Extensions.DependencyInjection;

namespace ChurchApp.Tests.Integration;

public class DonationFlowTests : IClassFixture<ApiTestFactory>
{
    private readonly ApiTestFactory _factory;

    public DonationFlowTests(ApiTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateDonation_ShouldPersistDonationSummaryAndAudit()
    {
        await ResetDatabaseAsync();
        var memberId = await SeedMemberAsync();
        var client = _factory.CreateClient();

        var createPayload = new CreateDonationRequest(
            memberId,
            null,
            DonationType.Tithe,
            DonationMethod.Cash,
            new DateOnly(2026, 3, 1),
            DonationAmount.Hundred,
            $"create-{Guid.NewGuid():N}",
            "test-runner",
            "Sunday Service",
            "Test donation");

        var response = await client.PostAsync(
            "/api/donations",
            new StringContent(
                JsonSerializer.Serialize(createPayload, AppJsonSerializerContext.Default.CreateDonationRequest),
                Encoding.UTF8,
                "application/json"));

        response.EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ChurchAppDbContext>();

        Assert.Equal(1, db.Donations.Count());
        Assert.Equal(1, db.DonationAudits.Count(x => x.Action == DonationAuditAction.Created));
        Assert.True(db.Summaries.Any(x => x.Type == SummaryType.Member && x.TotalAmount == 100m && x.DonationCount == 1));
        Assert.True(db.Summaries.Any(x => x.Type == SummaryType.Service && x.TotalAmount == 100m && x.DonationCount == 1));
    }

    [Fact]
    public async Task VoidDonation_ShouldRollbackSummaryAndCreateVoidAudit()
    {
        await ResetDatabaseAsync();
        var memberId = await SeedMemberAsync();
        var client = _factory.CreateClient();

        var createRequest = new CreateDonationRequest(
            memberId,
            null,
            DonationType.GeneralOffering,
            DonationMethod.Cash,
            new DateOnly(2026, 3, 1),
            DonationAmount.Fifty + DonationAmount.Fifty,
            $"void-{Guid.NewGuid():N}",
            "test-runner",
            "Sunday Service",
            null);

        var create = await client.PostAsync(
            "/api/donations",
            new StringContent(
                JsonSerializer.Serialize(createRequest, AppJsonSerializerContext.Default.CreateDonationRequest),
                Encoding.UTF8,
                "application/json"));

        create.EnsureSuccessStatusCode();
        using var versionScope = _factory.Services.CreateScope();
        var versionDb = versionScope.ServiceProvider.GetRequiredService<ChurchAppDbContext>();
        var createdDonation = versionDb.Donations.OrderByDescending(x => x.CreatedAtUtc).First();

        var voidRequest = new VoidDonationRequest(
            "Incorrect amount",
            "test-runner",
            createdDonation.Version);

        var voidResponse = await client.PostAsync(
            $"/api/donations/{createdDonation.Id}/void",
            new StringContent(
                JsonSerializer.Serialize(voidRequest, AppJsonSerializerContext.Default.VoidDonationRequest),
                Encoding.UTF8,
                "application/json"));

        voidResponse.EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ChurchAppDbContext>();
        var donation = db.Donations.Single(x => x.Id == createdDonation.Id);

        Assert.Equal(DonationStatus.Voided, donation.Status);
        Assert.Equal(2, db.DonationAudits.Count(x => x.DonationId == donation.Id));
        Assert.True(db.Summaries.Any(x => x.Type == SummaryType.Member && x.DonationCount == 0));
    }

    private async Task<Guid> SeedMemberAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ChurchAppDbContext>();

        var member = new ChurchApp.Application.Domain.Members.Member
        {
            Id = Guid.NewGuid(),
            FirstName = "Test",
            LastName = "Member",
            Email = $"{Guid.NewGuid():N}@example.com"
        };

        db.Members.Add(member);
        await db.SaveChangesAsync();
        return member.Id;
    }

    private async Task ResetDatabaseAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ChurchAppDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();
    }
}
