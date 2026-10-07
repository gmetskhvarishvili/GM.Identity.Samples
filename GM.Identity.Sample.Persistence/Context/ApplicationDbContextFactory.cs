using GM.EntityFramework.Domain.Common;
using GM.EntityFramework.Domain.Events;
using GM.Identity.Sample.Persistence.Infrastructure;
using Microsoft.EntityFrameworkCore;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Persistence.Context;

public class ApplicationDbContextFactory : DesignTimeDbContextFactoryBase<ApplicationDbContext>
{
    protected override ApplicationDbContext CreateNewInstance(DbContextOptions<ApplicationDbContext> options)
    {
        // Design-time (migrations) has no request, so no ambient tenant — a no-op actor is sufficient to
        // build the model and its tenant query filter. Migrations never save aggregates, so domain events
        // are never raised here; a no-op dispatcher satisfies the constructor.
        return new ApplicationDbContext(options, new DesignTimeCurrentActor(), new NoOpDomainEventDispatcher());
    }

    private sealed class NoOpDomainEventDispatcher : IDomainEventDispatcher
    {
        public Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task DispatchRangeAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class DesignTimeCurrentActor : ICurrentActor
    {
        public Guid? UserId => null;
        public Guid? ClientId => null;
        public Guid? TenantId => null;
        public Guid? SessionId => null;
        public string? ChannelId => null;
        public string? Culture => null;
        public string? IpAddress => null;
        public string? CorrelationId => null;
        public string? UserAgent => null;
        public string? IdempotencyKey => null;
    }
}
