using GM.Exceptions;
using GM.Identity.Authorization;
using GM.Identity.Sample.Application.Users.Commands.UpdateUserPassword;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserPasswordHistoryAggregate;
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
using ValidationException = GM.Exceptions.ValidationException;

namespace GM.Identity.Sample.Tests;

/// <summary>
/// End-to-end proof of password-reuse prevention: a password change cannot reuse the current password or a
/// recently used one, but an unused password is accepted. Requires Postgres + Redis; no-ops if unavailable.
/// </summary>
public sealed class PasswordHistoryEndToEndTests : IAsyncLifetime
{
    private const string First = "Correct123!";
    private const string Second = "Different456!";
    private const string Third = "Another789!";

    private readonly GmWebApplicationFactory<Program> _factory = new();
    private Guid _userId;
    private bool _infraReady;

    public async Task InitializeAsync()
    {
        try
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = User.Create($"pwhist-{Guid.NewGuid():N}", $"pwhist-{Guid.NewGuid():N}@test.local", null);
            var (hash, salt) = PasswordHasher.Hash(First);
            user.UpdatePassword(hash, salt);
            await context.Set<User>().AddAsync(user);
            // Seed the initial password into history (CreateUserCommand does this; this test creates the user directly).
            await context.Set<UserPasswordHistory>().AddAsync(
                UserPasswordHistory.Create(user.Id, hash, salt, DateTime.UtcNow));
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
                await context.Set<UserPasswordHistory>().IgnoreQueryFilters().Where(x => x.UserId == _userId).ExecuteDeleteAsync();
                await context.Set<User>().IgnoreQueryFilters().Where(u => u.Id == _userId).ExecuteDeleteAsync();
            }
            catch { /* best-effort cleanup */ }
        }
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Password_change_rejects_reuse_of_the_current_and_recent_passwords()
    {
        if (!_infraReady) return;

        // Change to a new password → recorded in history.
        await ChangeAsync(Second);

        // Reusing the (now current) password is rejected.
        await Assert.ThrowsAsync<ValidationException>(() => ChangeAsync(Second));

        // Reusing the original (recent history) password is rejected.
        await Assert.ThrowsAsync<ValidationException>(() => ChangeAsync(First));

        // A never-used password is accepted.
        await ChangeAsync(Third);
    }

    private async Task ChangeAsync(string newPassword)
    {
        using var scope = _factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        await mediator.Send(new UpdateUserPasswordCommand { Id = _userId, Password = newPassword });
    }
}
