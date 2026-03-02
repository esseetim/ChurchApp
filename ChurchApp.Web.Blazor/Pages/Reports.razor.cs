using Microsoft.AspNetCore.Components;
using ChurchApp.Web.Blazor.Models;
using ChurchApp.Web.Blazor.Services;
using Radzen;

namespace ChurchApp.Web.Blazor.Pages;

public partial class Reports : ComponentBase
{
    [Inject]
    private IReportService ReportService { get; set; } = default!;

    [Inject]
    private NotificationService NotificationService { get; set; } = default!;

    private ReportFormModel reportModel = new();
    private TimeRangeReportResponse? report;
    private bool isLoading = false;

    public async Task GenerateReport()
    {
        isLoading = true;
        try
        {
            report = await ReportService.GetTimeRangeReportAsync(
                reportModel.StartDate.ToString("yyyy-MM-dd"),
                reportModel.EndDate.ToString("yyyy-MM-dd"),
                reportModel.PersistReport
            );

            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Success,
                Summary = "Success",
                Detail = "Report generated successfully",
                Duration = 4000
            });
        }
        catch (Exception ex)
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Error",
                Detail = ex.Message,
                Duration = 6000
            });
        }
        finally
        {
            isLoading = false;
        }
    }

    public string GetDonationTypeLabel(DonationType type) => type switch
    {
        DonationType.GeneralOffering => "General Offering",
        DonationType.Tithe => "Tithe",
        DonationType.BuildingFund => "Building Fund",
        _ => type.ToString()
    };

    private class ReportFormModel
    {
        public DateTime StartDate { get; set; } = DateTime.Today;
        public DateTime EndDate { get; set; } = DateTime.Today;
        public bool PersistReport { get; set; } = true;
    }
}
