using Microsoft.AspNetCore.Components;
using ChurchApp.Web.Blazor.Models;
using ChurchApp.Web.Blazor.Services;
using Radzen;
using Radzen.Blazor;

namespace ChurchApp.Web.Blazor.Pages;

public partial class Ledger : ComponentBase
{
    [Inject]
    private IDonationService DonationService { get; set; } = default!;

    [Inject]
    private NotificationService NotificationService { get; set; } = default!;

    [Inject]
    private DialogService DialogService { get; set; } = default!;

    private RadzenDataGrid<DonationLedgerItem>? grid;
    private List<DonationLedgerItem> donations = new();
    private DateTime startDate = DateTime.Today;
    private DateTime endDate = DateTime.Today;
    private bool includeVoided = false;
    private bool isLoading = false;

    // Dialog state
    public string VoidReason { get; set; } = string.Empty;
    private DonationLedgerItem? donationToVoid;

    protected override async Task OnInitializedAsync()
    {
        await LoadDonations();
    }

    public async Task LoadDonations()
    {
        isLoading = true;
        try
        {
            var response = await DonationService.GetDonationsAsync(
                page: 1,
                pageSize: 200,
                startDate: startDate.ToString("yyyy-MM-dd"),
                endDate: endDate.ToString("yyyy-MM-dd"),
                includeVoided: includeVoided
            );

            donations = response.Donations.ToList();

            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Info,
                Summary = "Loaded",
                Detail = $"Found {donations.Count} donation(s)",
                Duration = 3000
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

    public async Task VoidDonationClick(DonationLedgerItem donation)
    {
        donationToVoid = donation;
        VoidReason = string.Empty;
        
        var confirmed = await DialogService.Confirm(
            $"Are you sure you want to void this donation of ${donation.Amount:F2}?",
            "Void Donation",
            new ConfirmOptions { OkButtonText = "Yes", CancelButtonText = "No" }
        );

        if (confirmed == true)
        {
            await ShowVoidReasonDialog();
        }
    }

    private async Task ShowVoidReasonDialog()
    {
        if (donationToVoid == null) return;

        var result = await DialogService.OpenAsync("Enter Void Reason",
            ds => BuildVoidDialogContent(ds),
            new DialogOptions { Width = "500px" }
        );

        if (result == true && !string.IsNullOrWhiteSpace(VoidReason))
        {
            await ProcessVoidDonation(donationToVoid, VoidReason);
            donationToVoid = null;
            VoidReason = string.Empty;
        }
    }

    private async Task ProcessVoidDonation(DonationLedgerItem donation, string reason)
    {
        try
        {
            var request = new VoidDonationRequest(
                Reason: reason,
                EnteredBy: "volunteer",
                ExpectedVersion: donation.Version
            );

            await DonationService.VoidDonationAsync(donation.Id, request);

            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Success,
                Summary = "Success",
                Detail = "Donation voided successfully",
                Duration = 4000
            });

            await LoadDonations();
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
    }

    public string GetDonationTypeLabel(DonationType type) => type switch
    {
        DonationType.GeneralOffering => "General Offering",
        DonationType.Tithe => "Tithe",
        DonationType.BuildingFund => "Building Fund",
        _ => type.ToString()
    };

    public string GetDonationMethodLabel(DonationMethod method) => method switch
    {
        DonationMethod.Cash => "Cash",
        DonationMethod.CashApp => "CashApp",
        DonationMethod.Zelle => "Zelle",
        DonationMethod.Check => "Check",
        DonationMethod.Card => "Card",
        DonationMethod.Other => "Other",
        _ => method.ToString()
    };
}
