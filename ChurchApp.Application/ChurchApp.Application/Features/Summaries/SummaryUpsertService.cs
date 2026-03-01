using ChurchApp.Application.Domain.Donations;
using ChurchApp.Application.Domain.Reports;
using ChurchApp.Application.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.Application.Features.Summaries;

public sealed class SummaryUpsertService(ChurchAppDbContext dbContext) : ISummaryUpsertService
{
    public async Task UpsertForDonationAsync(DonationCreatedDomainEvent donationEvent, CancellationToken cancellationToken)
    {
        var normalizedServiceName = string.IsNullOrWhiteSpace(donationEvent.ServiceName)
            ? null
            : donationEvent.ServiceName.Trim();

        if (normalizedServiceName is not null)
        {
            await UpsertSummaryAsync(
                SummaryType.Service,
                SummaryPeriodType.Day,
                donationEvent.DonationDate,
                donationEvent.DonationDate,
                null,
                null,
                normalizedServiceName,
                donationEvent.Amount,
                cancellationToken);
        }

        foreach (var period in new[]
                 {
                     SummaryPeriodType.Day,
                     SummaryPeriodType.Month,
                     SummaryPeriodType.Quarter,
                     SummaryPeriodType.Year
                 })
        {
            var (startDate, endDate) = GetPeriodRange(period, donationEvent.DonationDate);

            await UpsertSummaryAsync(
                SummaryType.Member,
                period,
                startDate,
                endDate,
                donationEvent.MemberId,
                null,
                null,
                donationEvent.Amount,
                cancellationToken);
        }

        var familyIds = await dbContext.FamilyMembers
            .Where(x => x.MemberId == donationEvent.MemberId)
            .Select(x => x.FamilyId)
            .ToListAsync(cancellationToken);

        foreach (var familyId in familyIds)
        {
            foreach (var period in new[]
                     {
                         SummaryPeriodType.Day,
                         SummaryPeriodType.Month,
                         SummaryPeriodType.Quarter,
                         SummaryPeriodType.Year
                     })
            {
                var (startDate, endDate) = GetPeriodRange(period, donationEvent.DonationDate);

                await UpsertSummaryAsync(
                    SummaryType.Family,
                    period,
                    startDate,
                    endDate,
                    null,
                    familyId,
                    null,
                    donationEvent.Amount,
                    cancellationToken);
            }
        }
    }

    private async Task UpsertSummaryAsync(
        SummaryType type,
        SummaryPeriodType periodType,
        DateOnly startDate,
        DateOnly endDate,
        Guid? memberId,
        Guid? familyId,
        string? serviceName,
        decimal amount,
        CancellationToken cancellationToken)
    {
        var summary = await dbContext.Summaries.SingleOrDefaultAsync(
            x => x.Type == type
                 && x.PeriodType == periodType
                 && x.StartDate == startDate
                 && x.EndDate == endDate
                 && x.MemberId == memberId
                 && x.FamilyId == familyId
                 && x.ServiceName == serviceName,
            cancellationToken);

        if (summary is null)
        {
            summary = new Summary
            {
                Id = Guid.NewGuid(),
                Type = type,
                PeriodType = periodType,
                StartDate = startDate,
                EndDate = endDate,
                MemberId = memberId,
                FamilyId = familyId,
                ServiceName = serviceName,
                TotalAmount = 0,
                DonationCount = 0,
                BreakdownJson = "{}",
                GeneratedAtUtc = DateTime.UtcNow
            };

            dbContext.Summaries.Add(summary);
        }

        summary.TotalAmount += amount;
        summary.DonationCount += 1;
        summary.GeneratedAtUtc = DateTime.UtcNow;
    }

    private static (DateOnly StartDate, DateOnly EndDate) GetPeriodRange(SummaryPeriodType periodType, DateOnly date)
    {
        return periodType switch
        {
            SummaryPeriodType.Day => (date, date),
            SummaryPeriodType.Month => (new DateOnly(date.Year, date.Month, 1), new DateOnly(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month))),
            SummaryPeriodType.Quarter => GetQuarterRange(date),
            SummaryPeriodType.Year => (new DateOnly(date.Year, 1, 1), new DateOnly(date.Year, 12, 31)),
            _ => (date, date)
        };
    }

    private static (DateOnly StartDate, DateOnly EndDate) GetQuarterRange(DateOnly date)
    {
        var quarter = ((date.Month - 1) / 3) + 1;
        var startMonth = ((quarter - 1) * 3) + 1;
        var endMonth = startMonth + 2;

        return (
            new DateOnly(date.Year, startMonth, 1),
            new DateOnly(date.Year, endMonth, DateTime.DaysInMonth(date.Year, endMonth)));
    }
}
