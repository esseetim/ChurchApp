using Microsoft.AspNetCore.Components;
using ChurchApp.Web.Blazor.Models;
using ChurchApp.Web.Blazor.Services;
using Radzen;
using System.ComponentModel.DataAnnotations;

namespace ChurchApp.Web.Blazor.Pages;

public partial class DonationDesk : ComponentBase
{
    [Inject]
    private IDonationService DonationService { get; set; } = default!;

    [Inject]
    private IMemberService MemberService { get; set; } = default!;

    [Inject]
    private IFamilyService FamilyService { get; set; } = default!;

    [Inject]
    private NotificationService NotificationService { get; set; } = default!;

    private List<MemberDisplay> members = new();
    private List<Family> families = new();
    private DonationFormModel donationModel = new();
    private bool IsSubmittingDonation { get; set; }

    private readonly Dictionary<DonationType, string> donationTypes = new()
    {
        { DonationType.GeneralOffering, "General Offering" },
        { DonationType.Tithe, "Tithe" },
        { DonationType.BuildingFund, "Building Fund" }
    };

    private readonly Dictionary<DonationMethod, string> donationMethods = new()
    {
        { DonationMethod.Cash, "Cash" },
        { DonationMethod.CashApp, "CashApp" },
        { DonationMethod.Zelle, "Zelle" },
        { DonationMethod.Check, "Check" },
        { DonationMethod.Card, "Card" },
        { DonationMethod.Other, "Other" }
    };

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

            members = memberResponse.Members
                .Select(m => new MemberDisplay
                {
                    Id = m.Id,
                    DisplayName = $"{m.FirstName} {m.LastName}"
                })
                .ToList();

            families = familyResponse.Families.ToList();
        }
        catch (Exception ex)
        {
            ShowError($"Failed to load data: {ex.Message}");
        }
    }

    public async Task OnMemberCreatedHandler(CreateMemberResponse response)
    {
        await LoadLookupData();
        donationModel.MemberId = response.MemberId;
    }

    public async Task OnFamilyCreatedHandler(CreateFamilyResponse response)
    {
        await LoadLookupData();
        donationModel.FamilyId = response.FamilyId;
    }

    private async Task HandleDonationSubmit()
    {
        if (!donationModel.MemberId.HasValue)
        {
            ShowWarning("Please select a member");
            return;
        }

        IsSubmittingDonation = true;
        try
        {
            var request = new CreateDonationRequest(
                MemberId: donationModel.MemberId.Value,
                DonationAccountId: null,
                Type: donationModel.Type,
                Method: donationModel.Method,
                DonationDate: donationModel.DonationDate.ToString("yyyy-MM-dd"),
                Amount: donationModel.Amount,
                IdempotencyKey: Guid.NewGuid().ToString(),
                EnteredBy: string.IsNullOrWhiteSpace(donationModel.EnteredBy) ? "volunteer" : donationModel.EnteredBy,
                ServiceName: string.IsNullOrWhiteSpace(donationModel.ServiceName) ? null : donationModel.ServiceName,
                Notes: string.IsNullOrWhiteSpace(donationModel.Notes) ? null : donationModel.Notes
            );

            var response = await DonationService.CreateDonationAsync(request);

            ShowSuccess($"Donation recorded: ${donationModel.Amount:F2}");

            // Reset form
            donationModel = new DonationFormModel
            {
                DonationDate = DateTime.Today,
                Type = DonationType.GeneralOffering,
                Method = DonationMethod.Cash,
                ServiceName = "Sunday Service",
                EnteredBy = "volunteer"
            };
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
        finally
        {
            IsSubmittingDonation = false;
        }
    }

    private async Task LinkMemberToFamily()
    {
        if (!donationModel.MemberId.HasValue || !donationModel.FamilyId.HasValue)
            return;

        try
        {
            var request = new AddFamilyMemberRequest(donationModel.MemberId.Value);
            await FamilyService.AddFamilyMemberAsync(donationModel.FamilyId.Value, request);

            ShowSuccess("Member linked to family");
            await LoadLookupData();
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    // Notification helpers following DRY principle
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

    // View models - keep internal to the page
    private class MemberDisplay
    {
        public Guid Id { get; set; }
        public string DisplayName { get; set; } = string.Empty;
    }

    private class DonationFormModel
    {
        public Guid? MemberId { get; set; }
        public Guid? FamilyId { get; set; }
        public DonationType Type { get; set; } = DonationType.GeneralOffering;
        public DonationMethod Method { get; set; } = DonationMethod.Cash;
        public DateTime DonationDate { get; set; } = DateTime.Today;
        
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; } = 0;
        
        public string ServiceName { get; set; } = "Sunday Service";
        public string EnteredBy { get; set; } = "volunteer";
        public string? Notes { get; set; }
    }
}
