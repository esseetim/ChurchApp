using Microsoft.AspNetCore.Components;
using ChurchApp.Web.Blazor.Models;

namespace ChurchApp.Web.Blazor.Components.Shared;

public partial class MemberSelector : ComponentBase
{
    [Parameter]
    public List<Member> Members { get; set; } = new();

    [Parameter]
    public Guid? SelectedMemberId { get; set; }

    [Parameter]
    public EventCallback<Guid?> SelectedMemberIdChanged { get; set; }

    [Parameter]
    public EventCallback<Member?> OnMemberSelected { get; set; }

    private async Task OnMemberChanged(object value)
    {
        var memberId = value as Guid?;
        await SelectedMemberIdChanged.InvokeAsync(memberId);
        
        var selectedMember = memberId.HasValue 
            ? Members.FirstOrDefault(m => m.Id == memberId.Value)
            : null;
        
        await OnMemberSelected.InvokeAsync(selectedMember);
    }
}
