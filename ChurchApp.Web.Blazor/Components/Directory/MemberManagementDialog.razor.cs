using System.Collections.Frozen;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using ChurchApp.Web.Blazor.Models;
using ChurchApp.Web.Blazor.Services;
using Radzen;

namespace ChurchApp.Web.Blazor.Components.Directory;

public partial class MemberManagementDialog : ComponentBase
{
    [Parameter] public Member Member { get; set; } = null!;

    [Inject] private IMemberService MemberService { get; set; } = null!;
    [Inject] private IFamilyService FamilyService { get; set; } = null!;
    [Inject] private DialogService DialogService { get; set; } = null!;
    [Inject] private NotificationService NotificationService { get; set; } = null!;

    private MemberProfileModel Profile { get; set; } = new();
    private NewAccountModel NewAccount { get; set; } = new();
    private List<DonationAccount> Accounts { get; set; } = [];
    private List<MemberFamily> MemberFamilies { get; set; } = [];
    private Dictionary<Guid, string> EditableHandles { get; } = [];
    private Dictionary<Guid, string?> EditableDisplayNames { get; } = [];
    private bool IsSavingProfile { get; set; }
    private bool _hasChanges;

    private static readonly FrozenDictionary<DonationMethod, string> DonationAccountMethods = FrozenDictionary.Create(
        new KeyValuePair<DonationMethod, string>(DonationMethod.CashApp, "CashApp"),
        new KeyValuePair<DonationMethod, string>(DonationMethod.Zelle, "Zelle"),
        new KeyValuePair<DonationMethod, string>(DonationMethod.Check, "Check"),
        new KeyValuePair<DonationMethod, string>(DonationMethod.Card, "Card"),
        new KeyValuePair<DonationMethod, string>(DonationMethod.Other, "Other"));

    protected override async Task OnInitializedAsync()
    {
        Profile = new MemberProfileModel
        {
            FirstName = Member.FirstName,
            LastName = Member.LastName,
            Email = Member.Email,
            PhoneNumber = Member.PhoneNumber
        };

        await LoadMemberDataAsync();
    }

    private async Task LoadMemberDataAsync()
    {
        try
        {
            var accountsResponse = await MemberService.GetDonationAccountsAsync(Member.Id);
            Accounts = [.. accountsResponse.Accounts];
            EditableHandles.Clear();
            EditableDisplayNames.Clear();

            foreach (var account in Accounts)
            {
                EditableHandles[account.Id] = account.Handle;
                EditableDisplayNames[account.Id] = account.DisplayName;
            }

            var familiesResponse = await MemberService.GetMemberFamiliesAsync(Member.Id);
            MemberFamilies = [.. familiesResponse.Families];
        }
        catch (Exception ex)
        {
            Accounts = [];
            MemberFamilies = [];
            NotifyError($"Failed to load member details: {ex.Message}");
        }
    }

    private async Task SaveProfileAsync()
    {
        IsSavingProfile = true;
        try
        {
            await MemberService.UpdateMemberAsync(Member.Id, new UpdateMemberRequest(
                Profile.FirstName.Trim(),
                Profile.LastName.Trim(),
                string.IsNullOrWhiteSpace(Profile.Email) ? null : Profile.Email.Trim(),
                string.IsNullOrWhiteSpace(Profile.PhoneNumber) ? null : Profile.PhoneNumber.Trim()));

            _hasChanges = true;
            NotifySuccess("Profile updated.");
        }
        catch (Exception ex)
        {
            NotifyError(ex.Message);
        }
        finally
        {
            IsSavingProfile = false;
        }
    }

    private async Task CreateAccountAsync()
    {
        if (string.IsNullOrWhiteSpace(NewAccount.Handle))
        {
            NotifyError("Handle is required.");
            return;
        }

        try
        {
            await MemberService.CreateDonationAccountAsync(Member.Id, new CreateDonationAccountRequest(
                NewAccount.Method,
                NewAccount.Handle.Trim(),
                string.IsNullOrWhiteSpace(NewAccount.DisplayName) ? null : NewAccount.DisplayName.Trim()));

            NewAccount = new NewAccountModel();
            _hasChanges = true;
            await LoadMemberDataAsync();
            NotifySuccess("Donation account added.");
        }
        catch (Exception ex)
        {
            NotifyError(ex.Message);
        }
    }

    private async Task UpdateAccountAsync(DonationAccount account)
    {
        try
        {
            await MemberService.UpdateDonationAccountAsync(
                Member.Id,
                account.Id,
                new UpdateDonationAccountRequest(
                    EditableHandles[account.Id],
                    EditableDisplayNames[account.Id],
                    true));

            _hasChanges = true;
            await LoadMemberDataAsync();
            NotifySuccess("Donation account updated.");
        }
        catch (Exception ex)
        {
            NotifyError(ex.Message);
        }
    }

    private async Task DeleteAccountAsync(DonationAccount account)
    {
        try
        {
            await MemberService.DeleteDonationAccountAsync(Member.Id, account.Id);
            _hasChanges = true;
            await LoadMemberDataAsync();
            NotifySuccess("Donation account removed.");
        }
        catch (Exception ex)
        {
            NotifyError(ex.Message);
        }
    }

    private void CloseDialog()
    {
        DialogService.Close(_hasChanges);
    }

    private void NotifySuccess(string message) =>
        NotificationService.Notify(new NotificationMessage
        {
            Severity = NotificationSeverity.Success,
            Summary = "Success",
            Detail = message,
            Duration = 3000
        });

    private void NotifyError(string message) =>
        NotificationService.Notify(new NotificationMessage
        {
            Severity = NotificationSeverity.Error,
            Summary = "Error",
            Detail = message,
            Duration = 5000
        });

    private sealed class MemberProfileModel
    {
        [Required] public string FirstName { get; set; } = string.Empty;
        [Required] public string LastName { get; set; } = string.Empty;
        [EmailAddress] public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
    }

    private sealed class NewAccountModel
    {
        public DonationMethod Method { get; set; } = DonationMethod.CashApp;
        public string Handle { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
    }
}
