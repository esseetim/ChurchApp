using System.Collections.Frozen;
using Microsoft.AspNetCore.Components;
using ChurchApp.Web.Blazor.Models;
using ChurchApp.Web.Blazor.Services;
using Radzen;
using System.ComponentModel.DataAnnotations;

namespace ChurchApp.Web.Blazor.Pages;

public partial class DonationDesk : ComponentBase
{
    [Inject]
    private IDonationService DonationService { get; set; } = null!;

    [Inject]
    private IMemberService MemberService { get; set; } = null!;

    [Inject]
    private IFamilyService FamilyService { get; set; } = null!;

    [Inject]
    private NotificationService NotificationService { get; set; } = null!;

    private List<MemberDisplay> _members = [];
    private List<Family> _families = [];
    private DonationFormModel _donationModel = new();
    private bool IsSubmittingDonation { get; set; }

    private readonly FrozenDictionary<DonationType, string> _donationTypes = FrozenDictionary.Create(
        new KeyValuePair<DonationType, string>(DonationType.GeneralOffering, "General Offering"), 
        new KeyValuePair<DonationType, string>(DonationType.Tithe, "Tithe"), 
        new KeyValuePair<DonationType, string>(DonationType.BuildingFund, "Building Fund"));

    private readonly FrozenDictionary<DonationMethod, string> _donationMethods = FrozenDictionary.Create(
        new KeyValuePair<DonationMethod, string>(DonationMethod.Cash, "Cash"), 
        new KeyValuePair<DonationMethod, string>(DonationMethod.CashApp, "CashApp"), 
        new KeyValuePair<DonationMethod, string>(DonationMethod.Zelle, "Zelle"), 
        new KeyValuePair<DonationMethod, string>(DonationMethod.Check, "Check"), 
        new KeyValuePair<DonationMethod, string>(DonationMethod.Card, "Card"), 
        new KeyValuePair<DonationMethod, string>(DonationMethod.Other, "Other"));

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

            _members = memberResponse.Members
                .Select(m => new MemberDisplay
                {
                    Id = m.Id,
                    DisplayName = $"{m.FirstName} {m.LastName}"
                })
                .ToList();

            _families = familyResponse.Families.ToList();
        }
        catch (Exception ex)
        {
            ShowError($"Failed to load data: {ex.Message}");
        }
    }

    public async Task OnMemberCreatedHandler(CreateMemberResponse response)
    {
        await LoadLookupData();
        _donationModel.MemberId = response.MemberId;
    }

    public async Task OnFamilyCreatedHandler(CreateFamilyResponse response)
    {
        await LoadLookupData();
        _donationModel.FamilyId = response.FamilyId;
    }

    private async Task HandleDonationSubmit()
    {
        if (!_donationModel.MemberId.HasValue)
        {
            ShowWarning("Please select a member");
            return;
        }

        IsSubmittingDonation = true;
        try
        {
            var request = new CreateDonationRequest(
                MemberId: _donationModel.MemberId.Value,
                DonationAccountId: null,
                Type: _donationModel.Type,
                Method: _donationModel.Method,
                DonationDate: _donationModel.DonationDate.ToString("yyyy-MM-dd"),
                Amount: _donationModel.Amount,
                IdempotencyKey: Guid.NewGuid().ToString(),
                EnteredBy: string.IsNullOrWhiteSpace(_donationModel.EnteredBy) ? "volunteer" : _donationModel.EnteredBy,
                ServiceName: string.IsNullOrWhiteSpace(_donationModel.ServiceName) ? null : _donationModel.ServiceName,
                Notes: string.IsNullOrWhiteSpace(_donationModel.Notes) ? null : _donationModel.Notes
            );

            var response = await DonationService.CreateDonationAsync(request);

            ShowSuccess($"Donation recorded: ${_donationModel.Amount:F2}");

            // Reset form
            _donationModel = new DonationFormModel
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
        if (!_donationModel.MemberId.HasValue || !_donationModel.FamilyId.HasValue)
            return;

        try
        {
            var request = new AddFamilyMemberRequest(_donationModel.MemberId.Value);
            await FamilyService.AddFamilyMemberAsync(_donationModel.FamilyId.Value, request);

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
