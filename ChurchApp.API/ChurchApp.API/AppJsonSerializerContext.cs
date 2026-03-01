using System.Text.Json.Serialization;
using ChurchApp.API.Endpoints;
using ChurchApp.API.Endpoints.Contracts;
using FastEndpoints;

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
[JsonSerializable(typeof(ErrorResponse))]
[JsonSerializable(typeof(HealthResponse))]
[JsonSerializable(typeof(CreateDonationRequest))]
[JsonSerializable(typeof(CreateDonationResponse))]
[JsonSerializable(typeof(VoidDonationRequest))]
[JsonSerializable(typeof(VoidDonationResponse))]
[JsonSerializable(typeof(GetDonationsRequest))]
[JsonSerializable(typeof(DonationLedgerResponse))]
[JsonSerializable(typeof(DonationLedgerItemDto))]
[JsonSerializable(typeof(List<DonationLedgerItemDto>))]
[JsonSerializable(typeof(GetServiceSummariesRequest))]
[JsonSerializable(typeof(GetMemberSummariesRequest))]
[JsonSerializable(typeof(GetFamilySummariesRequest))]
[JsonSerializable(typeof(SummaryDonationsRequest))]
[JsonSerializable(typeof(SummaryDonationsResponse))]
[JsonSerializable(typeof(TimeRangeReportRequest))]
[JsonSerializable(typeof(SummariesResponse))]
[JsonSerializable(typeof(SummaryItemDto))]
[JsonSerializable(typeof(List<SummaryItemDto>))]
[JsonSerializable(typeof(TimeRangeReportResponse))]
[JsonSerializable(typeof(DonationTypeBreakdownDto))]
[JsonSerializable(typeof(List<DonationTypeBreakdownDto>))]
public partial class AppJsonSerializerContext : JsonSerializerContext;
