using ChurchApp.Web.Blazor.Models;

namespace ChurchApp.Web.Blazor.Services;

public interface IReportService
{
    Task<SummariesResponse> GetServiceSummariesAsync(string serviceName, string startDate, string endDate, 
        bool persistReport = false, CancellationToken cancellationToken = default);
    Task<SummariesResponse> GetMemberSummariesAsync(Guid memberId, int? periodType = null, string? startDate = null, 
        string? endDate = null, bool persistReport = false, CancellationToken cancellationToken = default);
    Task<SummariesResponse> GetFamilySummariesAsync(Guid familyId, int? periodType = null, string? startDate = null, 
        string? endDate = null, bool persistReport = false, CancellationToken cancellationToken = default);
    Task<TimeRangeReportResponse> GetTimeRangeReportAsync(string startDate, string endDate, 
        bool persistReport = false, CancellationToken cancellationToken = default);
}
