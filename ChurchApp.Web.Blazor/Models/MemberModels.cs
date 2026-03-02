using System.Collections.Immutable;

namespace ChurchApp.Web.Blazor.Models;

public record Member(
    Guid Id,
    string FirstName,
    string LastName,
    string? Email,
    string? PhoneNumber
);

public record MembersResponse(
    int Page,
    int PageSize,
    int TotalCount,
    ImmutableArray<Member> Members
);

public record CreateMemberRequest(
    string FirstName,
    string LastName,
    string? Email = null,
    string? PhoneNumber = null
);

public record CreateMemberResponse(
    Guid MemberId
);
