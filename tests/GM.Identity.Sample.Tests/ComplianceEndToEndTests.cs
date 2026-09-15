using GM.EntityFramework.Persistence.Events;
using GM.Identity.Sample.Application.Users.Commands.RecordUserConsent;
using GM.Identity.Sample.Application.Users.Commands.RequestContactChange;
using GM.Identity.Sample.Application.Users.Queries.GetUserConsents;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserConsentAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserPendingContactChangeAggregate;
using GM.Identity.Sample.Domain.Enums;
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
/// End-to-end proof of the compliance features: consent acceptance is recorded and listable; a pending contact
/// change is stored encrypted at rest but reads back in plaintext; and the audit-retention job reaps events
/// older than the retention window. Requires Postgres + Redis; no-ops if unavailable.
/// </summary>
public sealed class ComplianceEndToEndTests : IAsyncLifetime
{
    private readonly GmWebApplicationFactory<Program> _factory = new();
    private readonly string _newEmail = $"pii-{Guid.NewGuid():N}@test.local";
    private readonly string _probeEventType = $"RetentionProbe-{Guid.NewGuid():N}";
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
                await context.Set<UserPendingContactChange>().IgnoreQueryFilters().Where(x => x.UserId == _userId).ExecuteDeleteAsync();
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

        using (var scope = _factory.Services.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            await mediator.Send(new RecordUserConsentCommand
            {
                UserId = _userId,
                ConsentType = "TermsOfService",
                DocumentVersion = "2026-01",
            });
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var consents = await mediator.Send(new GetUserConsentsQuery { UserId = _userId });
            var consent = Assert.Single(consents);
            Assert.Equal("TermsOfService", consent.ConsentType);
            Assert.Equal("2026-01", consent.DocumentVersion);
        }
    }

    [Fact]
    public async Task Pending_contact_is_encrypted_at_rest_but_reads_back_plaintext()
    {
        if (!_infraReady) return;

        using (var scope = _factory.Services.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            await mediator.Send(new RequestContactChangeCommand
            {
                UserId = _userId,
                ConfirmationType = ConfirmationType.Email,
                NewContact = _newEmail,
            });
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Through EF (converter applied) the value round-trips to plaintext.
            var entity = await context.Set<UserPendingContactChange>().IgnoreQueryFilters()
                .FirstAsync(x => x.UserId == _userId);
            Assert.Equal(_newEmail, entity.NewContact);

            // The raw column is ciphertext, not the plaintext email.
            var conn = context.Database.GetDbConnection();
            await conn.OpenAsync();
            try
            {
                await using var cmd = conn.CreateCommand();
                cmd.CommandText =
                    $"SELECT \"NewContact\" FROM application.user_pending_contact_changes WHERE \"UserId\" = '{_userId}'";
                var raw = (string?)await cmd.ExecuteScalarAsync();
                Assert.False(string.IsNullOrEmpty(raw));
                Assert.NotEqual(_newEmail, raw);
            }
            finally
            {
                await conn.CloseAsync();
            }
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
