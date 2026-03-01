namespace ChurchApp.Application.Domain.Common;

public interface IHasDomainEvents
{
    List<IDomainEvent> DomainEvents { get; }
}
