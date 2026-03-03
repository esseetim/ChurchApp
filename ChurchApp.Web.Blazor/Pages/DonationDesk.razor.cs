using System.Collections.Frozen;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using ChurchApp.Web.Blazor.Components.Dialogs;
using ChurchApp.Web.Blazor.Models;
using ChurchApp.Web.Blazor.Services;
using Radzen;
using Radzen.Blazor;

namespace ChurchApp.Web.Blazor.Pages;

public partial class DonationDesk : ComponentBase
{
    [Inject] private IDonationService DonationService { get; set; } = null!;
    [Inject] private IMemberService MemberService { get; set; } = null!;
    [Inject] private IFamilyService FamilyService { get; set; } = null!;
    [Inject] private IObligationService ObligationService { get; set; } = null!;
    [Inject] private NotificationService NotificationService { get; set; } = null!;
    [Inject] private DialogService DialogService { get; set; } = null!;

    private List<MemberDisplay> _members = [];
    private List<Family> _families = [];
    private List<DonationAccountDisplay> _availableDonationAccounts = [];
    private List<ObligationDisplay> _availableObligations = [];
    private DonationFormModel _donationModel = new();
    private RadzenDropDown<Guid?>? _memberDropDown;
    private bool IsSubmittingDonation { get; set; }

    // Use strongly-typed display models instead of dictionaries for type safety
    private static readonly FrozenSet<DonationTypeDisplay> _donationTypes = new[]
    {
        new DonationTypeDisplay(DonationType.GeneralOffering, "General Offering"),
        new DonationTypeDisplay(DonationType.Tithe, "Tithe"),
        new DonationTypeDisplay(DonationType.BuildingFund, "Building Fund"),
        new DonationTypeDisplay(DonationType.PledgePayment, "Pledge Payment"),
        new DonationTypeDisplay(DonationType.ClubDuePayment, "Club Due Payment")
    }.ToFrozenSet();

    private static readonly FrozenSet<DonationMethodDisplay> _donationMethods = new[]
    {
        new DonationMethodDisplay(DonationMethod.Cash, "Cash"),
        new DonationMethodDisplay(DonationMethod.CashApp, "CashApp"),
        new DonationMethodDisplay(DonationMethod.Zelle, "Zelle"),
        new DonationMethodDisplay(DonationMethod.Check, "Check"),
        new DonationMethodDisplay(DonationMethod.Card, "Card"),
        new DonationMethodDisplay(DonationMethod.Other, "Other")
    }.ToFrozenSet();

    protected override async Task OnInitializedAsync() => await LoadLookupData();

    private async Task LoadLookupData()
    {
        try
        {
            var membersTask = MemberService.GetMembersAsync(page: 1, pageSize: 500);
            var familiesTask = FamilyService.GetFamiliesAsync(page: 1, pageSize: 500);
            await Task.WhenAll(membersTask, familiesTask);

            var memberResponse = await membersTask;
            var familyResponse = await familiesTask;

            _members = memberResponse.Members
                .Select(m => new MemberDisplay(m.Id, $"{m.FirstName} {m.LastName}"))
                .ToList();

            _families = familyResponse.Families.ToList();
        }
        catch (Exception ex)
        {
            ShowError($"Failed to load data: {ex.Message}");
        }
    }

    private async Task OpenCreateMemberDialogAsync()
    {
        var result = await DialogService.OpenAsync<MemberFormDialog>(
            "Add Member",
            options: new DialogOptions { Width = "900px", Height = "650px", Resizable = true, Draggable = true });

        if (result is CreateMemberResponse response)
        {
            await LoadLookupData();
            _donationModel.MemberId = response.MemberId;
            await OnMemberChangedAsync();
        }
    }

    private async Task OpenCreateFamilyDialogAsync()
    {
        var result = await DialogService.OpenAsync<FamilyFormDialog>(
            "Add Family",
            options: new DialogOptions { Width = "700px", Height = "560px", Resizable = true, Draggable = true });

        if (result is CreateFamilyResponse response)
        {
            await LoadLookupData();
            _donationModel.FamilyId = response.FamilyId;
        }
    }

    private async Task OnMemberChangedAsync()
    {
        await LoadDonationAccountsAsync();
        await LoadObligationsAsync();
    }

    private async Task OnMethodChangedAsync()
    {
        await LoadDonationAccountsAsync();
    }
    
    private async Task OnTypeChangedAsync()
    {
        // Load obligations when PledgePayment or ClubDuePayment is selected
        if (_donationModel.Type is DonationType.PledgePayment or DonationType.ClubDuePayment)
        {
            await LoadObligationsAsync();
        }
        else
        {
            // Clear obligation selection for other donation types
            _donationModel.ObligationId = null;
            _availableObligations.Clear();
        }
    }

    private async Task LoadDonationAccountsAsync()
    {
        _availableDonationAccounts.Clear();
        _donationModel.DonationAccountId = null;

        if (!_donationModel.MemberId.HasValue || !RequiresDonationAccount(_donationModel.Method))
        {
            return;
        }

        try
        {
            var response = await MemberService.GetDonationAccountsAsync(_donationModel.MemberId.Value);
            _availableDonationAccounts = response.Accounts
                .Where(x => x.Method == _donationModel.Method && x.IsActive)
                .Select(x => new DonationAccountDisplay(
                    x.Id,
                    x.Method,
                    string.IsNullOrWhiteSpace(x.DisplayName) ? x.Handle : $"{x.DisplayName} ({x.Handle})"))
                .ToList();

            if (_availableDonationAccounts.Count == 1)
            {
                _donationModel.DonationAccountId = _availableDonationAccounts[0].Id;
            }
        }
        catch (Exception ex)
        {
            ShowError($"Failed to load donation accounts: {ex.Message}");
        }
    }

    private static bool RequiresDonationAccount(DonationMethod method) => method != DonationMethod.Cash;

    /// <summary>
    /// Loads active obligations for the selected member.
    /// Only called when PledgePayment or ClubDuePayment is selected.
    /// </summary>
    private async Task LoadObligationsAsync()
    {
        _availableObligations.Clear();
        _donationModel.ObligationId = null;

        if (!_donationModel.MemberId.HasValue)
        {
            return;
        }

        try
        {
            var response = await ObligationService.GetMemberObligationsAsync(_donationModel.MemberId.Value);
            
            // Filter to active obligations matching the donation type
            var targetObligationType = _donationModel.Type == DonationType.PledgePayment
                ? ObligationType.FundraisingPledge
                : ObligationType.ClubDue;

            _availableObligations = response.Obligations
                .Where(x => x.Status == ObligationStatus.Active && x.Type == targetObligationType)
                .Select(x => new ObligationDisplay(
                    x.Id,
                    x.Title,
                    $"{x.Title} - ${x.BalanceRemaining:F2} remaining of ${x.TotalAmount:F2}"))
                .ToList();

            // Auto-select if only one active obligation
            if (_availableObligations.Count == 1)
            {
                _donationModel.ObligationId = _availableObligations[0].Id;
            }
        }
        catch (Exception ex)
        {
            ShowError($"Failed to load obligations: {ex.Message}");
        }
    }

    private async Task HandleDonationSubmit()
    {
        if (!_donationModel.MemberId.HasValue)
        {
            ShowWarning("Please select a member");
            return;
        }

        if (RequiresDonationAccount(_donationModel.Method) && !_donationModel.DonationAccountId.HasValue)
        {
            ShowWarning("Please select a donation account for the selected method.");
            return;
        }

        // Validate obligation selection for pledge/due payments
        if (_donationModel.Type is DonationType.PledgePayment or DonationType.ClubDuePayment)
        {
            if (!_donationModel.ObligationId.HasValue)
            {
                ShowWarning("Please select an obligation for pledge or due payments.");
                return;
            }
        }

        IsSubmittingDonation = true;
        try
        {
            var request = new CreateDonationRequest(
                MemberId: _donationModel.MemberId.Value,
                DonationAccountId: _donationModel.DonationAccountId,
                Type: _donationModel.Type,
                Method: _donationModel.Method,
                DonationDate: _donationModel.DonationDate.ToString("yyyy-MM-dd"),
                Amount: _donationModel.Amount,
                IdempotencyKey: Guid.NewGuid().ToString(),
                EnteredBy: string.IsNullOrWhiteSpace(_donationModel.EnteredBy) ? "volunteer" : _donationModel.EnteredBy,
                ServiceName: string.IsNullOrWhiteSpace(_donationModel.ServiceName) ? null : _donationModel.ServiceName,
                Notes: string.IsNullOrWhiteSpace(_donationModel.Notes) ? null : _donationModel.Notes,
                ObligationId: _donationModel.ObligationId);

            await DonationService.CreateDonationAsync(request);
            ShowSuccess($"Donation recorded: ${_donationModel.Amount:F2}");

            var stickyDate = _donationModel.DonationDate;
            var stickyServiceName = _donationModel.ServiceName;
            var stickyEnteredBy = _donationModel.EnteredBy;
            var stickyMethod = _donationModel.Method;
            var stickyType = _donationModel.Type;

            _donationModel = new DonationFormModel
            {
                DonationDate = stickyDate,
                Type = stickyType,
                Method = stickyMethod,
                ServiceName = stickyServiceName,
                EnteredBy = stickyEnteredBy
            };

            _availableDonationAccounts.Clear();
            await _memberDropDown!.FocusAsync();
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
        {
            return;
        }

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

    private sealed record MemberDisplay(Guid Id, string DisplayName);

    private sealed record DonationAccountDisplay(Guid Id, DonationMethod Method, string DisplayName);

    private sealed record ObligationDisplay(Guid Id, string Title, string DisplayName);

    /// <summary>
    /// Display model for DonationType dropdown - provides type safety and encapsulation.
    /// Following SRP: Single responsibility is to represent a displayable donation type.
    /// </summary>
    private sealed record DonationTypeDisplay(DonationType Value, string DisplayName);

    /// <summary>
    /// Display model for DonationMethod dropdown - provides type safety and encapsulation.
    /// Following SRP: Single responsibility is to represent a displayable donation method.
    /// </summary>
    private sealed record DonationMethodDisplay(DonationMethod Value, string DisplayName);

    private sealed class DonationFormModel
    {
        public Guid? MemberId { get; set; }
        public Guid? FamilyId { get; set; }
        public Guid? DonationAccountId { get; set; }
        public Guid? ObligationId { get; set; }
        public DonationType Type { get; set; } = DonationType.GeneralOffering;
        public DonationMethod Method { get; set; } = DonationMethod.Cash;
        public DateTime DonationDate { get; set; } = DateTime.Today;

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }

        public string ServiceName { get; set; } = "Sunday Service";
        public string EnteredBy { get; set; } = "volunteer";
        public string? Notes { get; set; }
    }
}
