using Microsoft.AspNetCore.Components;
using Radzen;

namespace ChurchApp.Web.Blazor.Components.Shared;

public partial class StatusMessage : ComponentBase
{
    [Inject]
    private NotificationService NotificationService { get; set; } = default!;

    public void ShowSuccess(string message)
    {
        NotificationService.Notify(new NotificationMessage
        {
            Severity = NotificationSeverity.Success,
            Summary = "Success",
            Detail = message,
            Duration = 4000
        });
    }

    public void ShowError(string message)
    {
        NotificationService.Notify(new NotificationMessage
        {
            Severity = NotificationSeverity.Error,
            Summary = "Error",
            Detail = message,
            Duration = 6000
        });
    }

    public void ShowInfo(string message)
    {
        NotificationService.Notify(new NotificationMessage
        {
            Severity = NotificationSeverity.Info,
            Summary = "Info",
            Detail = message,
            Duration = 3000
        });
    }
}
