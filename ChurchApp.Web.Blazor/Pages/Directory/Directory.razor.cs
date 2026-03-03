using Microsoft.AspNetCore.Components;
using ChurchApp.Web.Blazor.Components.Dialogs;
using ChurchApp.Web.Blazor.Components.Directory;
using ChurchApp.Web.Blazor.Models;
using ChurchApp.Web.Blazor.Services;
using Radzen;

namespace ChurchApp.Web.Blazor.Pages.Directory;

public partial class Directory : ComponentBase
{
    [Inject] private IMemberService MemberService { get; set; } = null!;
    [Inject] private IFamilyService FamilyService { get; set; } = null!;
    [Inject] private DialogService DialogService { get; set; } = null!;
    [Inject] private NotificationService NotificationService { get; set; } = null!;

    private List<Member> Members { get; set; } = [];
    private List<Family> Families { get; set; } = [];
    private Dictionary<Guid, int> MemberAccountCounts { get; } = [];

    protected override async Task OnInitializedAsync()
    {
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        var membersResponse = await MemberService.GetMembersAsync(page: 1, pageSize: 500);
        var familiesResponse = await FamilyService.GetFamiliesAsync(page: 1, pageSize: 500);

        Members = membersResponse.Members.ToList();
        Families = familiesResponse.Families.ToList();

        MemberAccountCounts.Clear();
        var accountTasks = Members.Select(async member =>
        {
            var accountsResponse = await MemberService.GetDonationAccountsAsync(member.Id);
            return new KeyValuePair<Guid, int>(member.Id, accountsResponse.Accounts.Length);
        });

        foreach (var pair in await Task.WhenAll(accountTasks))
        {
            MemberAccountCounts[pair.Key] = pair.Value;
        }
    }

    private int GetAccountCount(Guid memberId) => MemberAccountCounts.TryGetValue(memberId, out var count) ? count : 0;

    private async Task CreateMemberAsync()
    {
        var result = await DialogService.OpenAsync<MemberFormDialog>(
            "Add Member",
            options: new DialogOptions { Width = "900px", Height = "650px", Resizable = true, Draggable = true });

        if (result is CreateMemberResponse)
        {
            await LoadDataAsync();
        }
    }

    private async Task CreateFamilyAsync()
    {
        var result = await DialogService.OpenAsync<FamilyFormDialog>(
            "Add Family",
            options: new DialogOptions { Width = "700px", Height = "560px", Resizable = true, Draggable = true });

        if (result is CreateFamilyResponse)
        {
            await LoadDataAsync();
        }
    }

    private async Task OpenMemberAsync(Member member)
    {
        var result = await DialogService.OpenAsync<MemberManagementDialog>(
            $"Manage {member.FirstName} {member.LastName}",
            new Dictionary<string, object> { ["Member"] = member },
            new DialogOptions { Width = "980px", Height = "700px", Resizable = true, Draggable = true });

        if (result is true)
        {
            await LoadDataAsync();
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Success,
                Summary = "Saved",
                Detail = "Member changes saved.",
                Duration = 3500
            });
        }
    }

    private async Task OpenFamilyAsync(Family family)
    {
        var result = await DialogService.OpenAsync<FamilyManagementDialog>(
            $"Manage {family.Name}",
            new Dictionary<string, object> { ["Family"] = family },
            new DialogOptions { Width = "880px", Height = "680px", Resizable = true, Draggable = true });

        if (result is true)
        {
            await LoadDataAsync();
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Success,
                Summary = "Saved",
                Detail = "Family changes saved.",
                Duration = 3500
            });
        }
    }
}
