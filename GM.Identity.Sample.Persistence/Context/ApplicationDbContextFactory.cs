using GM.EntityFramework.Domain.Common;
using GM.Identity.Sample.Persistence.Infrastructure;
using Microsoft.EntityFrameworkCore;

using System;

namespace GM.Identity.Sample.Persistence.Context;

public class ApplicationDbContextFactory : DesignTimeDbContextFactoryBase<ApplicationDbContext>
{
    protected override ApplicationDbContext CreateNewInstance(DbContextOptions<ApplicationDbContext> options)
    {
        // Design-time (migrations) has no request, so no ambient tenant — a no-op actor is sufficient to
        // build the model and its tenant query filter.
        return new ApplicationDbContext(options, new DesignTimeCurrentActor());
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
