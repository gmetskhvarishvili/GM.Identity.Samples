using GM.Exceptions;
using GM.Identity.Sample.Application.Users.Commands.CreateUser;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserAggregate;
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
/// End-to-end proof that the configurable password policy is enforced on account creation: passwords failing
/// the default policy (min 8 + upper/lower/digit) are rejected; a compliant one is accepted. Requires
/// Postgres + Redis; no-ops if they aren't reachable.
/// </summary>
public sealed class PasswordPolicyEndToEndTests : IAsyncLifetime
{
    private readonly GmWebApplicationFactory<Program> _factory = new();
    private Guid _createdUserId;
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
        if (_infraReady && _createdUserId != Guid.Empty)
        {
            try
            {
                using var scope = _factory.Services.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await context.Set<User>().IgnoreQueryFilters().Where(u => u.Id == _createdUserId).ExecuteDeleteAsync();
            }
            catch { /* best-effort cleanup */ }
        }
        await _factory.DisposeAsync();
    }

    [Theory]
    [InlineData("short1A")]        // too short (7)
    [InlineData("alllower123")]    // no uppercase
    [InlineData("ALLUPPER123")]    // no lowercase
    [InlineData("NoDigitsHere")]   // no digit
    public async Task Weak_passwords_are_rejected(string weak)
    {
        if (!_infraReady) return;

        await Assert.ThrowsAsync<ValidationException>(() => CreateAsync(weak));
    }

    [Fact]
    public async Task A_compliant_password_is_accepted()
    {
        if (!_infraReady) return;

        _createdUserId = await CreateAsync("Compliant123");
        Assert.NotEqual(Guid.Empty, _createdUserId);
    }

    private async Task<Guid> CreateAsync(string password)
    {
        using var scope = _factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var suffix = Guid.NewGuid().ToString("N");
        return await mediator.Send(new CreateUserCommand
        {
            Username = $"pwpolicy-{suffix}",
            Email = $"pwpolicy-{suffix}@test.local",
            Password = password,
        });
    }
}
