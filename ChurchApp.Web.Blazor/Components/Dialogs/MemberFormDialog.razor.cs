using System.Collections.Frozen;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using ChurchApp.Web.Blazor.Models;
using ChurchApp.Web.Blazor.Services;
using Radzen;

namespace ChurchApp.Web.Blazor.Components.Dialogs;

public partial class MemberFormDialog : ComponentBase
{
    [Inject] private IMemberService MemberService { get; set; } = null!;
    [Inject] private DialogService DialogService { get; set; } = null!;
    [Inject] private NotificationService NotificationService { get; set; } = null!;

    private MemberFormModel Model { get; } = new();
    private bool IsSubmitting { get; set; }

    private static readonly FrozenDictionary<DonationMethod, string> DonationAccountMethods = FrozenDictionary.Create(
        new KeyValuePair<DonationMethod, string>(DonationMethod.CashApp, "CashApp"),
        new KeyValuePair<DonationMethod, string>(DonationMethod.Zelle, "Zelle"),
        new KeyValuePair<DonationMethod, string>(DonationMethod.Check, "Check"),
        new KeyValuePair<DonationMethod, string>(DonationMethod.Card, "Card"),
        new KeyValuePair<DonationMethod, string>(DonationMethod.Other, "Other"));

    private void AddAccountRow()
    {
        Model.DonationAccounts.Add(new DonationAccountFormModel());
    }

    private void RemoveAccountRow(int index)
    {
        if (index >= 0 && index < Model.DonationAccounts.Count)
        {
            Model.DonationAccounts.RemoveAt(index);
        }
    }

    private async Task HandleSubmitAsync()
    {
        IsSubmitting = true;
        try
        {
            var donationAccounts = Model.DonationAccounts
                .Where(x => !string.IsNullOrWhiteSpace(x.Handle))
                .Select(x => new CreateDonationAccountRequest(
                    x.Method,
                    x.Handle.Trim(),
                    string.IsNullOrWhiteSpace(x.DisplayName) ? null : x.DisplayName.Trim()))
                .ToList();

            var request = new CreateMemberRequest(
                Model.FirstName.Trim(),
                Model.LastName.Trim(),
                string.IsNullOrWhiteSpace(Model.Email) ? null : Model.Email.Trim(),
                string.IsNullOrWhiteSpace(Model.PhoneNumber) ? null : Model.PhoneNumber.Trim(),
                donationAccounts);

            var result = await MemberService.CreateMemberAsync(request);
            DialogService.Close(result);
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
            IsSubmitting = false;
        }
    }

    private void Cancel()
    {
        DialogService.Close(null);
    }

    private sealed class MemberFormModel
    {
        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;

        [EmailAddress]
        public string? Email { get; set; }

        public string? PhoneNumber { get; set; }

        public List<DonationAccountFormModel> DonationAccounts { get; } = [];
    }

    private sealed class DonationAccountFormModel
    {
        public DonationMethod Method { get; set; } = DonationMethod.CashApp;
        public string Handle { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
    }
}
