using ChurchApp.Application.Domain.Common;
using ChurchApp.Application.Infrastructure.DomainEvents;

namespace ChurchApp.Application.Infrastructure.Transactions;

public sealed class EfUnitOfWork(ChurchAppDbContext dbContext, IDomainEventDispatcher domainEventDispatcher) : IUnitOfWork
{
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        var domainEvents = dbContext.ChangeTracker
            .Entries<IHasDomainEvents>()
            .SelectMany(x => x.Entity.DomainEvents)
            .ToList();

        foreach (var entry in dbContext.ChangeTracker.Entries<IHasDomainEvents>())
        {
            entry.Entity.DomainEvents.Clear();
        }

        foreach (var domainEvent in domainEvents)
        {
            await domainEventDispatcher.DispatchAsync(domainEvent, cancellationToken);
        }

        return await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        await operation(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }
}
