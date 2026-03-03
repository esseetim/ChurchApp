using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using ChurchApp.Web.Blazor.Models;

namespace ChurchApp.Web.Blazor.Serialization;

/// <summary>
/// Source-generated JSON serialization context for all API contracts.
/// Provides AOT-friendly, reflection-free JSON serialization.
/// Performance: ~2-3x faster than reflection-based serialization.
/// </summary>
[JsonSerializable(typeof(CreateDonationRequest))]
[JsonSerializable(typeof(CreateDonationResponse))]
[JsonSerializable(typeof(DonationLedgerItem))]
[JsonSerializable(typeof(DonationLedgerResponse))]
[JsonSerializable(typeof(VoidDonationRequest))]
[JsonSerializable(typeof(Member))]
[JsonSerializable(typeof(MembersResponse))]
[JsonSerializable(typeof(CreateMemberRequest))]
[JsonSerializable(typeof(CreateMemberResponse))]
[JsonSerializable(typeof(CreateDonationAccountRequest))]
[JsonSerializable(typeof(DonationAccount))]
[JsonSerializable(typeof(MemberDonationAccountsResponse))]
[JsonSerializable(typeof(Family))]
[JsonSerializable(typeof(FamiliesResponse))]
[JsonSerializable(typeof(CreateFamilyRequest))]
[JsonSerializable(typeof(CreateFamilyResponse))]
[JsonSerializable(typeof(AddFamilyMemberRequest))]
[JsonSerializable(typeof(SummaryItem))]
[JsonSerializable(typeof(SummariesResponse))]
[JsonSerializable(typeof(DonationTypeBreakdown))]
[JsonSerializable(typeof(TimeRangeReportResponse))]
[JsonSerializable(typeof(ImmutableArray<DonationLedgerItem>))]
[JsonSerializable(typeof(ImmutableArray<Member>))]
[JsonSerializable(typeof(ImmutableArray<DonationAccount>))]
[JsonSerializable(typeof(ImmutableArray<Family>))]
[JsonSerializable(typeof(ImmutableArray<SummaryItem>))]
[JsonSerializable(typeof(ImmutableArray<DonationTypeBreakdown>))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = false,
    GenerationMode = JsonSourceGenerationMode.Metadata | JsonSourceGenerationMode.Serialization
)]
public partial class ChurchAppJsonContext : JsonSerializerContext;
