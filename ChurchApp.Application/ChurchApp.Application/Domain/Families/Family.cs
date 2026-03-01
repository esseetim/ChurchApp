namespace ChurchApp.Application.Domain.Families;

public class Family
{
    public Guid Id { get; set; }
    public required string Name { get; set; }

    public ICollection<FamilyMember> Members { get; set; } = new List<FamilyMember>();
}
