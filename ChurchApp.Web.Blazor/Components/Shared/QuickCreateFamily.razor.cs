using Microsoft.AspNetCore.Components;
using ChurchApp.Web.Blazor.Models;
using ChurchApp.Web.Blazor.Services;
using Radzen;
using System.ComponentModel.DataAnnotations;

namespace ChurchApp.Web.Blazor.Components.Shared;

public partial class QuickCreateFamily : ComponentBase
{
    [Parameter]
    public EventCallback<CreateFamilyResponse> OnFamilyCreated { get; set; }

    [Inject]
    private IFamilyService FamilyService { get; set; } = default!;

    [Inject]
    private NotificationService NotificationService { get; set; } = default!;

    private QuickCreateFamilyModel model = new();
    private bool IsSubmitting { get; set; }

    private async Task HandleSubmit()
    {
        IsSubmitting = true;
        try
        {
            var request = new CreateFamilyRequest(model.Name);
            var response = await FamilyService.CreateFamilyAsync(request);
            
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Success,
                Summary = "Success",
                Detail = $"Family created: {model.Name}",
                Duration = 4000
            });

            // Reset form
            model = new QuickCreateFamilyModel();
            
            await OnFamilyCreated.InvokeAsync(response);
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

    private class QuickCreateFamilyModel
    {
        [Required(ErrorMessage = "Family name is required")]
        public string Name { get; set; } = string.Empty;
    }
}
