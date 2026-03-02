using Microsoft.AspNetCore.Components;
using ChurchApp.Web.Blazor.Models;
using ChurchApp.Web.Blazor.Services;
using Radzen;
using System.ComponentModel.DataAnnotations;

namespace ChurchApp.Web.Blazor.Components.Shared;

public partial class QuickCreateMember : ComponentBase
{
    [Parameter]
    public EventCallback<CreateMemberResponse> OnMemberCreated { get; set; }

    [Inject]
    private IMemberService MemberService { get; set; } = default!;

    [Inject]
    private NotificationService NotificationService { get; set; } = default!;

    private QuickCreateMemberModel model = new();
    private bool IsSubmitting { get; set; }

    private async Task HandleSubmit()
    {
        IsSubmitting = true;
        try
        {
            var request = new CreateMemberRequest(
                model.FirstName,
                model.LastName
            );

            var response = await MemberService.CreateMemberAsync(request);
            
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Success,
                Summary = "Success",
                Detail = $"Member created: {model.FirstName} {model.LastName}",
                Duration = 4000
            });

            // Reset form
            model = new QuickCreateMemberModel();
            
            await OnMemberCreated.InvokeAsync(response);
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

    private class QuickCreateMemberModel
    {
        [Required(ErrorMessage = "First name is required")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")]
        public string LastName { get; set; } = string.Empty;
    }
}
