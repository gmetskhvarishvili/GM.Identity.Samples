using GM.Exceptions;
using GM.Identity;
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
using System.Threading;
using System.Threading.Tasks;
using ValidationException = GM.Exceptions.ValidationException;

namespace GM.Identity.Sample.Tests;

/// <summary>
/// End-to-end proof that a breached password is refused. A stub breach-checker (injected via ReplaceService)
/// flags one specific password; creating a user with it is rejected, while a clean password is accepted.
/// Requires Postgres + Redis; no-ops if unavailable.
/// </summary>
public sealed class BreachedPasswordEndToEndTests : IAsyncLifetime
{
    private const string BreachedPassword = "Breached123";

    private readonly GmWebApplicationFactory<Program> _factory =
        new GmWebApplicationFactory<Program>().ReplaceService<IBreachedPasswordChecker>(new StubChecker());

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

    [Fact]
    public async Task A_breached_password_is_rejected_but_a_clean_one_is_accepted()
    {
        if (!_infraReady) return;

        await Assert.ThrowsAsync<ValidationException>(() => CreateAsync(BreachedPassword));

        _createdUserId = await CreateAsync("Wholesome123");
        Assert.NotEqual(Guid.Empty, _createdUserId);
    }

    private async Task<Guid> CreateAsync(string password)
    {
        using var scope = _factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var suffix = Guid.NewGuid().ToString("N");
        return await mediator.Send(new CreateUserCommand
        {
            Username = $"breach-{suffix}",
            Email = $"breach-{suffix}@test.local",
            Password = password,
        });
    }

    private sealed class StubChecker : IBreachedPasswordChecker
    {
        public Task<bool> IsBreachedAsync(string password, CancellationToken cancellationToken) =>
            Task.FromResult(password == BreachedPassword);
    }
}
