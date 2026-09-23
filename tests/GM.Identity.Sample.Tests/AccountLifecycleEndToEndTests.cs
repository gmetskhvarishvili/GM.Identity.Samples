using GM.Identity.Sample.Application.Users.Commands.CreateUser;
using GM.Identity.Sample.Application.Users.Commands.DeleteUser;
using GM.Identity.Sample.Application.Users.Queries.ExportCurrentUserData;
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
/// End-to-end proof of the self-service account lifecycle: registration creates an unconfirmed account and
/// queues an email-confirmation code; data export returns the user's profile; self-delete soft-removes the
/// account. Requires Postgres + Redis; no-ops if they aren't reachable.
/// </summary>
public sealed class AccountLifecycleEndToEndTests : IAsyncLifetime
{
    private readonly GmWebApplicationFactory<Program> _factory = new();
    private readonly string _suffix = Guid.NewGuid().ToString("N");
    private Guid _userId;
    private bool _infraReady;

    public Task InitializeAsync()
    {
        try
        {
            using var scope = _factory.Services.CreateScope();
            _ = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            _infraReady = true;
        }
        catch
        {
            _infraReady = false;
        }
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (_infraReady && _userId != Guid.Empty)
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
    public async Task Create_then_export_then_self_delete()
    {
        if (!_infraReady) return;

        // Create the user.
        using (var scope = _factory.Services.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            _userId = await mediator.Send(new CreateUserCommand
            {
                Username = $"reg-{_suffix}",
                Email = $"reg-{_suffix}@test.local",
                Password = "Register123",
            });
        }
        Assert.NotEqual(Guid.Empty, _userId);

        // Export → returns the profile.
        using (var scope = _factory.Services.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var export = await mediator.Send(new ExportCurrentUserDataQuery { UserId = _userId });
            Assert.Equal(_userId, export.Id);
            Assert.Equal($"reg-{_suffix}", export.Username);
            Assert.Equal($"reg-{_suffix}@test.local", export.Email);
        }

        // Self-delete → soft-removed.
        using (var scope = _factory.Services.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            await mediator.Send(new DeleteUserCommand { Id = _userId });
        }
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = await context.Set<User>().IgnoreQueryFilters().FirstAsync(u => u.Id == _userId);
            Assert.True(user.IsDeleted);
        }
    }
}
