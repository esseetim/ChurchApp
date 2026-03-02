using Microsoft.AspNetCore.Components;
using ChurchApp.Web.Blazor.Models;
using ChurchApp.Web.Blazor.Services;
using Radzen;

namespace ChurchApp.Web.Blazor.Pages;

public partial class Summaries : ComponentBase
{
    [Inject]
    private IReportService ReportService { get; set; } = default!;

    [Inject]
    private IMemberService MemberService { get; set; } = default!;

    [Inject]
    private IFamilyService FamilyService { get; set; } = default!;

    [Inject]
    private NotificationService NotificationService { get; set; } = default!;

    private List<MemberDisplay> members = [];
    private List<Family> families = [];
    private List<SummaryItem> summaries = [];
    private bool isLoading = false;

    private ServiceSummaryModel serviceSummaryModel = new();
    private MemberSummaryModel memberSummaryModel = new();
    private FamilySummaryModel familySummaryModel = new();

    protected override async Task OnInitializedAsync() => await LoadLookupData();

    private async Task LoadLookupData()
    {
        try
        {
            var membersTask = MemberService.GetMembersAsync(page: 1, pageSize: 200);
            var familiesTask = FamilyService.GetFamiliesAsync(page: 1, pageSize: 200);

            await Task.WhenAll(membersTask, familiesTask);

            var memberResponse = await membersTask;
            var familyResponse = await familiesTask;

            members = [.. memberResponse.Members
                .Select(m => new MemberDisplay
                {
                    Id = m.Id,
                    DisplayName = $"{m.FirstName} {m.LastName}"
                })];

            families = [.. familyResponse.Families];
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    public async Task RunServiceSummary()
    {
        isLoading = true;
        try
        {
            var response = await ReportService.GetServiceSummariesAsync(
                serviceSummaryModel.ServiceName,
                serviceSummaryModel.StartDate.ToString("yyyy-MM-dd"),
                serviceSummaryModel.EndDate.ToString("yyyy-MM-dd")
            );

            summaries = [.. response.Summaries];

            ShowSuccess($"Loaded {summaries.Count} service summaries");
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
        finally
        {
            isLoading = false;
        }
    }

    public async Task RunMemberSummary()
    {
        if (!memberSummaryModel.MemberId.HasValue)
        {
            ShowWarning("Please select a member");
            return;
        }

        isLoading = true;
        try
        {
            var response = await ReportService.GetMemberSummariesAsync(
                memberSummaryModel.MemberId.Value,
                startDate: memberSummaryModel.StartDate.ToString("yyyy-MM-dd"),
                endDate: memberSummaryModel.EndDate.ToString("yyyy-MM-dd")
            );

            summaries = [.. response.Summaries];

            ShowSuccess($"Loaded {summaries.Count} member summaries");
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
        finally
        {
            isLoading = false;
        }
    }

    public async Task RunFamilySummary()
    {
        if (!familySummaryModel.FamilyId.HasValue)
        {
            ShowWarning("Please select a family");
            return;
        }

        isLoading = true;
        try
        {
            var response = await ReportService.GetFamilySummariesAsync(
                familySummaryModel.FamilyId.Value,
                startDate: familySummaryModel.StartDate.ToString("yyyy-MM-dd"),
                endDate: familySummaryModel.EndDate.ToString("yyyy-MM-dd")
            );

            summaries = [.. response.Summaries];

            ShowSuccess($"Loaded {summaries.Count} family summaries");
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
        finally
        {
            isLoading = false;
        }
    }

    private void ShowSuccess(string message) =>
        NotificationService.Notify(new NotificationMessage
        {
            Severity = NotificationSeverity.Success,
            Summary = "Success",
            Detail = message,
            Duration = 4000
        });

    private void ShowError(string message) =>
        NotificationService.Notify(new NotificationMessage
        {
            Severity = NotificationSeverity.Error,
            Summary = "Error",
            Detail = message,
            Duration = 6000
        });

    private void ShowWarning(string message) =>
        NotificationService.Notify(new NotificationMessage
        {
            Severity = NotificationSeverity.Warning,
            Summary = "Validation",
            Detail = message,
            Duration = 4000
        });

    // View models
    private class MemberDisplay
    {
        public Guid Id { get; set; }
        public string DisplayName { get; set; } = string.Empty;
    }

    private class ServiceSummaryModel
    {
        public string ServiceName { get; set; } = "Sunday Service";
        public DateTime StartDate { get; set; } = DateTime.Today;
        public DateTime EndDate { get; set; } = DateTime.Today;
    }

    private class MemberSummaryModel
    {
        public Guid? MemberId { get; set; }
        public DateTime StartDate { get; set; } = DateTime.Today;
        public DateTime EndDate { get; set; } = DateTime.Today;
    }

    private class FamilySummaryModel
    {
        public Guid? FamilyId { get; set; }
        public DateTime StartDate { get; set; } = DateTime.Today;
        public DateTime EndDate { get; set; } = DateTime.Today;
    }
}
