using GM.Identity.Sample.Application.Users.Commands.BulkSetUserBlock;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserAggregate;
using GM.Identity.Sample.Persistence.Context;
using GM.Mediator.Contracts;
using GM.Testing;
using GM.Testing.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Tests;

/// <summary>
/// End-to-end proof of the bulk block/unblock admin operation. Requires Postgres + Redis; no-ops if unavailable.
/// </summary>
public sealed class BulkUserBlockEndToEndTests : IAsyncLifetime
{
    private readonly GmWebApplicationFactory<Program> _factory = new();
    private readonly List<Guid> _userIds = new();
    private bool _infraReady;

    public async Task InitializeAsync()
    {
        try
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            for (var i = 0; i < 3; i++)
            {
                var user = User.Create($"bulk-{Guid.NewGuid():N}", $"bulk-{Guid.NewGuid():N}@test.local", null);
                var (hash, salt) = PasswordHasher.Hash("Correct123!");
                user.UpdatePassword(hash, salt);
                await context.Set<User>().AddAsync(user);
                _userIds.Add(user.Id);
            }
            await context.SaveChangesAsync();
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
                await context.Set<User>().IgnoreQueryFilters().Where(u => _userIds.Contains(u.Id)).ExecuteDeleteAsync();
            }
            catch { /* best-effort cleanup */ }
        }
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Bulk_block_then_unblock_changes_all_users()
    {
        if (!_infraReady) return;

        var blocked = await SendAsync(new BulkSetUserBlockCommand { UserIds = _userIds, Block = true });
        Assert.Equal(_userIds.Count, blocked);
        Assert.True(await AllMatchAsync(u => u.IsBlocked));

        var unblocked = await SendAsync(new BulkSetUserBlockCommand { UserIds = _userIds, Block = false });
        Assert.Equal(_userIds.Count, unblocked);
        Assert.True(await AllMatchAsync(u => !u.IsBlocked));
    }

    private async Task<int> SendAsync(BulkSetUserBlockCommand command)
    {
        using var scope = _factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        return await mediator.Send(command);
    }

    private async Task<bool> AllMatchAsync(Func<User, bool> predicate)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var users = await context.Set<User>().IgnoreQueryFilters().Where(u => _userIds.Contains(u.Id)).ToListAsync();
        return users.Count == _userIds.Count && users.All(predicate);
    }
}
