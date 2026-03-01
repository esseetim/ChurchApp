using System.Text.Json.Serialization;
using ChurchApp.API.Endpoints;
using ChurchApp.API.Endpoints.Contracts;

namespace ChurchApp.API;

/// <summary>
/// JSON serializer context for AOT compilation.
/// Add your DTOs and response types here for source generation.
/// </summary>
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(Dictionary<string, object>))]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(HealthResponse))]
[JsonSerializable(typeof(CreateDonationRequest))]
[JsonSerializable(typeof(CreateDonationResponse))]
[JsonSerializable(typeof(GetServiceSummariesRequest))]
[JsonSerializable(typeof(GetMemberSummariesRequest))]
[JsonSerializable(typeof(GetFamilySummariesRequest))]
[JsonSerializable(typeof(TimeRangeReportRequest))]
[JsonSerializable(typeof(SummariesResponse))]
[JsonSerializable(typeof(SummaryItemDto))]
[JsonSerializable(typeof(List<SummaryItemDto>))]
[JsonSerializable(typeof(TimeRangeReportResponse))]
[JsonSerializable(typeof(DonationTypeBreakdownDto))]
[JsonSerializable(typeof(List<DonationTypeBreakdownDto>))]
public partial class AppJsonSerializerContext : JsonSerializerContext;
