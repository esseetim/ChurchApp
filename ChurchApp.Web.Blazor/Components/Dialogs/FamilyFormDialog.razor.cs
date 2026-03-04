using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using ChurchApp.Web.Blazor.Models;
using ChurchApp.Web.Blazor.Services;
using Radzen;

namespace ChurchApp.Web.Blazor.Components.Dialogs;

public partial class FamilyFormDialog : ComponentBase
{
    [Inject] private IFamilyService FamilyService { get; set; } = null!;
    [Inject] private IMemberService MemberService { get; set; } = null!;
    [Inject] private DialogService DialogService { get; set; } = null!;
    [Inject] private NotificationService NotificationService { get; set; } = null!;

    private FamilyFormModel Model { get; } = new();
    private List<MemberDisplay> Members { get; set; } = [];
    private bool IsSubmitting { get; set; }

    protected override async Task OnInitializedAsync()
    {
        var response = await MemberService.GetMembersAsync(page: 1, pageSize: 500);
        Members = [.. response.Members.Select(x => new MemberDisplay(x.Id, $"{x.FirstName} {x.LastName}"))];
    }

    private async Task HandleSubmitAsync()
    {
        IsSubmitting = true;
        try
        {
            var createResponse = await FamilyService.CreateFamilyAsync(new CreateFamilyRequest(Model.Name.Trim()));

            foreach (var memberId in Model.SelectedMemberIds)
            {
                await FamilyService.AddFamilyMemberAsync(createResponse.FamilyId, new AddFamilyMemberRequest(memberId));
            }

            DialogService.Close(createResponse);
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

    private sealed class FamilyFormModel
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        public IList<Guid> SelectedMemberIds { get; set; } = [];
    }

    private sealed record MemberDisplay(Guid Id, string DisplayName);
}
