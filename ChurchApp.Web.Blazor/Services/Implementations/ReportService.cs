using System.Net.Http.Json;
using System.Text.Json;
using ChurchApp.Web.Blazor.Models;
using ChurchApp.Web.Blazor.Serialization;

namespace ChurchApp.Web.Blazor.Services.Implementations;

public class ReportService(IHttpClientFactory httpClientFactory) : IReportService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        TypeInfoResolver = ChurchAppJsonContext.Default
    };

    public async Task<SummariesResponse> GetServiceSummariesAsync(
        string serviceName, 
        string startDate, 
        string endDate, 
        bool persistReport = false, 
        CancellationToken cancellationToken = default)
    {
        var queryString = $"serviceName={Uri.EscapeDataString(serviceName)}" +
                         $"&startDate={Uri.EscapeDataString(startDate)}" +
                         $"&endDate={Uri.EscapeDataString(endDate)}" +
                         $"&persistReport={persistReport.ToString().ToLower()}";
        
        using var httpClient = httpClientFactory.CreateClient("ChurchAppApi");
        return await httpClient.GetFromJsonAsync<SummariesResponse>(
            $"/api/reports/service-summaries?{queryString}", 
            JsonOptions, 
            cancellationToken)
            ?? throw new InvalidOperationException("Failed to deserialize response");
    }

    public async Task<SummariesResponse> GetMemberSummariesAsync(
        Guid memberId, 
        int? periodType = null, 
        string? startDate = null, 
        string? endDate = null, 
        bool persistReport = false, 
        CancellationToken cancellationToken = default)
    {
        var queryParams = new List<string> { $"memberId={memberId}" };
        
        if (periodType.HasValue) queryParams.Add($"periodType={periodType.Value}");
        if (!string.IsNullOrEmpty(startDate)) queryParams.Add($"startDate={Uri.EscapeDataString(startDate)}");
        if (!string.IsNullOrEmpty(endDate)) queryParams.Add($"endDate={Uri.EscapeDataString(endDate)}");
        queryParams.Add($"persistReport={persistReport.ToString().ToLower()}");
        
        var queryString = string.Join("&", queryParams);
        
        using var httpClient = httpClientFactory.CreateClient("ChurchAppApi");
        return await httpClient.GetFromJsonAsync<SummariesResponse>(
            $"/api/reports/member-summaries?{queryString}", 
            JsonOptions, 
            cancellationToken)
            ?? throw new InvalidOperationException("Failed to deserialize response");
    }

    public async Task<SummariesResponse> GetFamilySummariesAsync(
        Guid familyId, 
        int? periodType = null, 
        string? startDate = null, 
        string? endDate = null, 
        bool persistReport = false, 
        CancellationToken cancellationToken = default)
    {
        var queryParams = new List<string> { $"familyId={familyId}" };
        
        if (periodType.HasValue) queryParams.Add($"periodType={periodType.Value}");
        if (!string.IsNullOrEmpty(startDate)) queryParams.Add($"startDate={Uri.EscapeDataString(startDate)}");
        if (!string.IsNullOrEmpty(endDate)) queryParams.Add($"endDate={Uri.EscapeDataString(endDate)}");
        queryParams.Add($"persistReport={persistReport.ToString().ToLower()}");
        
        var queryString = string.Join("&", queryParams);
        
        using var httpClient = httpClientFactory.CreateClient("ChurchAppApi");
        return await httpClient.GetFromJsonAsync<SummariesResponse>(
            $"/api/reports/family-summaries?{queryString}", 
            JsonOptions, 
            cancellationToken)
            ?? throw new InvalidOperationException("Failed to deserialize response");
    }

    public async Task<TimeRangeReportResponse> GetTimeRangeReportAsync(
        string startDate, 
        string endDate, 
        bool persistReport = false, 
        CancellationToken cancellationToken = default)
    {
        var queryString = $"startDate={Uri.EscapeDataString(startDate)}" +
                         $"&endDate={Uri.EscapeDataString(endDate)}" +
                         $"&persistReport={persistReport.ToString().ToLower()}";
        
        using var httpClient = httpClientFactory.CreateClient("ChurchAppApi");
        return await httpClient.GetFromJsonAsync<TimeRangeReportResponse>(
            $"/api/reports/time-range?{queryString}", 
            JsonOptions, 
            cancellationToken)
            ?? throw new InvalidOperationException("Failed to deserialize response");
    }
}
