using Microsoft.AspNetCore.Components;

namespace ChurchApp.Web.Blazor.Components.Shared;

public partial class DateRangePicker : ComponentBase
{
    [Parameter]
    public DateTime? StartDate { get; set; }

    [Parameter]
    public EventCallback<DateTime?> StartDateChanged { get; set; }

    [Parameter]
    public DateTime? EndDate { get; set; }

    [Parameter]
    public EventCallback<DateTime?> EndDateChanged { get; set; }

    [Parameter]
    public EventCallback OnRangeChanged { get; set; }

    private async Task OnDateChanged()
    {
        await StartDateChanged.InvokeAsync(StartDate);
        await EndDateChanged.InvokeAsync(EndDate);
        await OnRangeChanged.InvokeAsync();
    }
}
