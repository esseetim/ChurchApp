using Microsoft.AspNetCore.Components;
using ChurchApp.Web.Blazor.Models;

namespace ChurchApp.Web.Blazor.Components.Shared;

public partial class FamilySelector : ComponentBase
{
    [Parameter]
    public List<Family> Families { get; set; } = new();

    [Parameter]
    public Guid? SelectedFamilyId { get; set; }

    [Parameter]
    public EventCallback<Guid?> SelectedFamilyIdChanged { get; set; }

    [Parameter]
    public EventCallback<Family?> OnFamilySelected { get; set; }

    private async Task OnFamilyChanged(object value)
    {
        var familyId = value as Guid?;
        await SelectedFamilyIdChanged.InvokeAsync(familyId);
        
        var selectedFamily = familyId.HasValue 
            ? Families.FirstOrDefault(f => f.Id == familyId.Value)
            : null;
        
        await OnFamilySelected.InvokeAsync(selectedFamily);
    }
}
