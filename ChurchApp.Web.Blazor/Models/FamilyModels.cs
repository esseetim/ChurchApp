using System.Collections.Immutable;

namespace ChurchApp.Web.Blazor.Models;

public record Family(
    Guid Id,
    string Name,
    int MemberCount
);

public record FamiliesResponse(
    int Page,
    int PageSize,
    int TotalCount,
    ImmutableArray<Family> Families
);

public record CreateFamilyRequest(
    string Name
);

public record CreateFamilyResponse(
    Guid FamilyId
);

public record AddFamilyMemberRequest(
    Guid MemberId
);
