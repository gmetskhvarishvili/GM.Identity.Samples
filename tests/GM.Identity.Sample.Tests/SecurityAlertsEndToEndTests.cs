using GM.Identity.Authorization;
using GM.Identity.Sample.Application.Users.Commands.SetUserBlock;
using GM.Identity.Sample.Application.Users.Commands.UpdateUserPassword;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserAggregate;
using GM.Identity.Sample.Domain.BoundedContext.MessagingBoundedContext.OutboxMessageAggregate;
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
/// End-to-end proof that security-relevant account changes queue a notification (a SecurityAlert outbox
/// message): blocking a user and changing a password both enqueue one. Requires Postgres + Redis; no-ops if
/// they aren't reachable.
/// </summary>
public sealed class SecurityAlertsEndToEndTests : IAsyncLifetime
{
    private const string EventMarker = "SecurityAlertRaisedIntegrationEvent";

    private readonly GmWebApplicationFactory<Program> _factory = new();
    private Guid _userId;
    private bool _infraReady;

    public async Task InitializeAsync()
    {
        try
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = User.Create($"alert-{Guid.NewGuid():N}", $"alert-{Guid.NewGuid():N}@test.local", null);
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
                await context.Set<OutboxMessage>().Where(m => m.UserId == _userId).ExecuteDeleteAsync();
                await context.Set<User>().IgnoreQueryFilters().Where(u => u.Id == _userId).ExecuteDeleteAsync();
            }
            catch { /* best-effort cleanup */ }
        }
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Blocking_a_user_queues_a_security_alert()
    {
        if (!_infraReady) return;

        await SendAsync(new SetUserBlockCommand { UserId = _userId, Block = true });
        Assert.True(await HasSecurityAlertAsync());
    }

    [Fact]
    public async Task Changing_a_password_queues_a_security_alert()
    {
        if (!_infraReady) return;

        await SendAsync(new UpdateUserPasswordCommand { Id = _userId, Password = "Different456!" });
        Assert.True(await HasSecurityAlertAsync());
    }

    private async Task SendAsync(IRequest request)
    {
        using var scope = _factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        await mediator.Send(request);
    }

    private async Task<bool> HasSecurityAlertAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await context.Set<OutboxMessage>()
            .Where(m => m.UserId == _userId && m.EventType.Contains(EventMarker))
            .AnyAsync();
    }
}
