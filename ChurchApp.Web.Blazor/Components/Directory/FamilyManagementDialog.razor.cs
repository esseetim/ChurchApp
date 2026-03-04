using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using ChurchApp.Web.Blazor.Models;
using ChurchApp.Web.Blazor.Services;
using Radzen;

namespace ChurchApp.Web.Blazor.Components.Directory;

public partial class FamilyManagementDialog : ComponentBase
{
    [Parameter] public Family Family { get; set; } = null!;

    [Inject] private IFamilyService FamilyService { get; set; } = null!;
    [Inject] private IMemberService MemberService { get; set; } = null!;
    [Inject] private DialogService DialogService { get; set; } = null!;
    [Inject] private NotificationService NotificationService { get; set; } = null!;

    private FamilyEditModel Model { get; set; } = new();
    private List<MemberDisplay> AllMembers { get; set; } = [];
    private bool IsSaving { get; set; }
    private bool _hasChanges;

    protected override async Task OnInitializedAsync()
    {
        Model.Name = Family.Name;

        var membersResponse = await MemberService.GetMembersAsync(page: 1, pageSize: 500);
        AllMembers = [.. membersResponse.Members.Select(x => new MemberDisplay(x.Id, $"{x.FirstName} {x.LastName}"))];

        var familyMembers = await FamilyService.GetFamilyMembersAsync(Family.Id);
        Model.SelectedMemberIds = [.. familyMembers.Members.Select(x => x.MemberId)];
    }

    private async Task HandleSaveAsync()
    {
        IsSaving = true;
        try
        {
            await FamilyService.UpdateFamilyAsync(Family.Id, new UpdateFamilyRequest(Model.Name.Trim()));

            var existing = await FamilyService.GetFamilyMembersAsync(Family.Id);
            var existingIds = existing.Members.Select(x => x.MemberId).ToHashSet();
            var selectedIds = Model.SelectedMemberIds.ToHashSet();

            var toAdd = selectedIds.Except(existingIds).ToList();
            var toRemove = existingIds.Except(selectedIds).ToList();

            foreach (var memberId in toAdd)
            {
                await FamilyService.AddFamilyMemberAsync(Family.Id, new AddFamilyMemberRequest(memberId));
            }

            foreach (var memberId in toRemove)
            {
                await FamilyService.RemoveFamilyMemberAsync(Family.Id, memberId);
            }

            _hasChanges = true;
            DialogService.Close(_hasChanges);
        }
        catch (Exception ex)
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Error",
                Detail = ex.Message,
                Duration = 5000
            });
        }
        finally
        {
            IsSaving = false;
        }
    }

    private void Cancel()
    {
        DialogService.Close(_hasChanges);
    }

    private sealed class FamilyEditModel
    {
        [Required] public string Name { get; set; } = string.Empty;
        public IList<Guid> SelectedMemberIds { get; set; } = [];
    }

    private sealed record MemberDisplay(Guid Id, string DisplayName);
}
