using ChurchApp.Application.Domain.Donations;
using ChurchApp.Application.Domain.Reports;

namespace ChurchApp.API.Endpoints.Contracts;

public sealed record CreateDonationRequest(
    Guid MemberId,
    Guid? DonationAccountId,
    DonationType Type,
    DonationMethod Method,
    DateOnly DonationDate,
    decimal Amount,
    string? IdempotencyKey,
    string? EnteredBy,
    string? ServiceName,
    string? Notes);

public sealed record CreateDonationResponse(Guid DonationId, int Version, bool IsDuplicate);

public sealed record VoidDonationRequest(string Reason, string? EnteredBy, int ExpectedVersion);

public sealed record VoidDonationResponse(Guid DonationId, int Version, DateTime VoidedAtUtc);

public sealed record GetDonationsRequest(
    int Page = 1,
    int PageSize = 50,
    DateOnly? StartDate = null,
    DateOnly? EndDate = null,
    Guid? MemberId = null,
    Guid? FamilyId = null,
    DonationType? Type = null,
    DonationMethod? Method = null,
    bool IncludeVoided = false);

public sealed record DonationLedgerItemDto(
    Guid Id,
    Guid MemberId,
    Guid? DonationAccountId,
    DonationType Type,
    DonationMethod Method,
    DateOnly DonationDate,
    decimal Amount,
    DonationStatus Status,
    string? ServiceName,
    string? Notes,
    DateTime CreatedAtUtc,
    string CreatedBy,
    DateTime? VoidedAtUtc,
    string? VoidedBy,
    string? VoidReason,
    int Version);

public sealed record DonationLedgerResponse(int Page, int PageSize, int TotalCount, List<DonationLedgerItemDto> Donations);

public sealed record GetServiceSummariesRequest(string ServiceName, DateOnly StartDate, DateOnly EndDate, bool PersistReport = false);

public sealed record GetMemberSummariesRequest(Guid MemberId, SummaryPeriodType? PeriodType, DateOnly? StartDate, DateOnly? EndDate, bool PersistReport = false);

public sealed record GetFamilySummariesRequest(Guid FamilyId, SummaryPeriodType? PeriodType, DateOnly? StartDate, DateOnly? EndDate, bool PersistReport = false);

public sealed record TimeRangeReportRequest(DateOnly StartDate, DateOnly EndDate, bool PersistReport = false);

public sealed record SummaryItemDto(
    Guid Id,
    SummaryType Type,
    SummaryPeriodType PeriodType,
    DateOnly StartDate,
    DateOnly EndDate,
    string? ServiceName,
    Guid? MemberId,
    Guid? FamilyId,
    decimal TotalAmount,
    int DonationCount,
    DateTime GeneratedAtUtc);

public sealed record SummariesResponse(List<SummaryItemDto> Summaries);

public sealed record SummaryDonationsRequest(Guid SummaryId, int Page = 1, int PageSize = 50);

public sealed record SummaryDonationsResponse(Guid SummaryId, int Page, int PageSize, int TotalCount, List<DonationLedgerItemDto> Donations);

public sealed record DonationTypeBreakdownDto(DonationType Type, decimal TotalAmount, int DonationCount);

public sealed record TimeRangeReportResponse(
    DateOnly StartDate,
    DateOnly EndDate,
    decimal TotalAmount,
    int DonationCount,
    List<DonationTypeBreakdownDto> Breakdown);

public sealed record GetMembersRequest(string? Search = null, int Page = 1, int PageSize = 50);

public sealed record CreateMemberRequest(
    string FirstName,
    string LastName,
    string? Email = null,
    string? PhoneNumber = null,
    List<CreateDonationAccountRequest>? DonationAccounts = null);

public sealed record CreateMemberResponse(Guid MemberId);

public sealed record UpdateMemberRequest(
    string FirstName,
    string LastName,
    string? Email = null,
    string? PhoneNumber = null);

public sealed record CreateDonationAccountRequest(
    DonationMethod Method,
    string Handle,
    string? DisplayName = null);

public sealed record DonationAccountDto(
    Guid Id,
    Guid MemberId,
    DonationMethod Method,
    string Handle,
    string? DisplayName,
    bool IsActive);

public sealed record MemberDonationAccountsResponse(Guid MemberId, List<DonationAccountDto> Accounts);

public sealed record UpdateDonationAccountRequest(
    string Handle,
    string? DisplayName = null,
    bool IsActive = true);

public sealed record MemberListItemDto(
    Guid Id,
    string FirstName,
    string LastName,
    string? Email,
    string? PhoneNumber);

public sealed record MembersResponse(int Page, int PageSize, int TotalCount, List<MemberListItemDto> Members);

public sealed record GetFamiliesRequest(string? Search = null, int Page = 1, int PageSize = 50);

public sealed record CreateFamilyRequest(string Name);

public sealed record UpdateFamilyRequest(string Name);

public sealed record CreateFamilyResponse(Guid FamilyId);

public sealed record FamilyListItemDto(Guid Id, string Name, int MemberCount);

public sealed record FamiliesResponse(int Page, int PageSize, int TotalCount, List<FamilyListItemDto> Families);

public sealed record AddFamilyMemberRequest(Guid MemberId);

public sealed record AddFamilyMemberResponse(Guid FamilyId, Guid MemberId, bool Added);

public sealed record MemberFamilyDto(Guid FamilyId, string FamilyName);

public sealed record MemberFamiliesResponse(Guid MemberId, List<MemberFamilyDto> Families);

public sealed record FamilyMemberDto(Guid MemberId, string FirstName, string LastName, string? Email, string? PhoneNumber);

public sealed record FamilyMembersResponse(Guid FamilyId, List<FamilyMemberDto> Members);
