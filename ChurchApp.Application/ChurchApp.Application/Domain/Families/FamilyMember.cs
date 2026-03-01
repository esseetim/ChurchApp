using ChurchApp.Application.Domain.Members;

namespace ChurchApp.Application.Domain.Families;

public class FamilyMember
{
    public Guid FamilyId { get; set; }
    public Family Family { get; set; } = null!;

    public Guid MemberId { get; set; }
    public Member Member { get; set; } = null!;
}
