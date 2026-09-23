using GM.EntityFramework.Persistence.Events;
using GM.Identity.Sample.Application.Users.Commands.RecordUserConsent;
using GM.Identity.Sample.Application.Users.Queries.GetUserConsents;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.ConsentDocumentAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserConsentAggregate;
using GM.Identity.Sample.Infrastructure.Maintenance;
using GM.Identity.Sample.Persistence.Context;
using GM.Mediator.Contracts;
using GM.Testing;
using GM.Testing.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

using System;
using System.Linq;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Tests;

/// <summary>
/// End-to-end proof of the compliance features: consent acceptance is recorded and listable; and the
/// audit-retention job reaps events older than the retention window. Requires Postgres + Redis; no-ops if
/// unavailable.
/// </summary>
public sealed class ComplianceEndToEndTests : IAsyncLifetime
{
    private readonly GmWebApplicationFactory<Program> _factory = new();
    private readonly string _probeEventType = $"RetentionProbe-{Guid.NewGuid():N}";
    private readonly string _consentType = $"ToS-{Guid.NewGuid():N}";
    private Guid _userId;
    private bool _infraReady;

    public async Task InitializeAsync()
    {
        try
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = User.Create($"compliance-{Guid.NewGuid():N}", $"compliance-{Guid.NewGuid():N}@test.local", null);
            var (hash, salt) = PasswordHasher.Hash("Correct123!");
            user.UpdatePassword(hash, salt);
            await context.Set<User>().AddAsync(user);
            await context.SaveChangesAsync();
            _userId = user.Id;
            _infraReady = true;
        }
        catch
        {
            _infraReady = false;
        }
    }

    public async Task DisposeAsync()
    {
        if (_infraReady)
        {
            try
            {
                using var scope = _factory.Services.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await context.Set<UserConsent>().IgnoreQueryFilters().Where(x => x.UserId == _userId).ExecuteDeleteAsync();
                await context.Set<ConsentDocument>().IgnoreQueryFilters().Where(x => x.ConsentType == _consentType).ExecuteDeleteAsync();
                await context.DomainEvents.Where(x => x.EventType == _probeEventType).ExecuteDeleteAsync();
                await context.Set<User>().IgnoreQueryFilters().Where(u => u.Id == _userId).ExecuteDeleteAsync();
            }
            catch { /* best-effort cleanup */ }
        }
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Consent_acceptance_is_recorded_and_listable()
    {
        if (!_infraReady) return;

        // A matching document must exist in the registry (non-mandatory so it can't gate other login tests).
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await context.Set<ConsentDocument>()
                .AddAsync(ConsentDocument.Create(_consentType, "Terms of Service", "body", "2026-01", isMandatory: false));
            await context.SaveChangesAsync();
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            await mediator.Send(new RecordUserConsentCommand
            {
                UserId = _userId,
                ConsentType = _consentType,
                DocumentVersion = "2026-01",
            });
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var consents = await mediator.Send(new GetUserConsentsQuery { UserId = _userId });
            var consent = Assert.Single(consents);
            Assert.Equal(_consentType, consent.ConsentType);
            Assert.Equal("2026-01", consent.DocumentVersion);
        }
    }

    [Fact]
    public async Task Audit_retention_job_reaps_events_older_than_the_window()
    {
        if (!_infraReady) return;

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.DomainEvents.Add(new StoredDomainEvent
            {
                Id = Guid.NewGuid(),
                AggregateType = "User",
                AggregateId = _userId.ToString(),
                EventType = _probeEventType,
                Payload = "{}",
                OccurredOn = DateTime.UtcNow.AddDays(-400), // older than the 365-day default retention
            });
            await context.SaveChangesAsync();
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var job = ActivatorUtilities.CreateInstance<AuditRetentionPurgeJob>(scope.ServiceProvider);
            await job.ExecuteAsync(null!, default);
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.False(await context.DomainEvents.AnyAsync(x => x.EventType == _probeEventType));
        }
    }
}
