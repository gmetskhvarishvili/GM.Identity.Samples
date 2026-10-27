using GM.Identity.Sample.Application.Users.Commands.CreateUser;
using GM.Identity.Sample.Application.Users.Commands.UpdateUser;
using GM.Identity.Sample.Application.Users.Queries.GetUserDetails;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserAggregate;
using GM.Identity.Sample.Persistence.Context;
using GM.Mediator.Contracts;
using GM.Testing.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

using System;
using System.Linq;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Tests;

/// <summary>
/// End-to-end proof of the user's profile image key: set at creation, returned in the details, kept by an update
/// that omits it, replaced by one that sends a new key, and cleared by an empty one. Requires Postgres + Redis;
/// no-ops if they aren't reachable.
/// </summary>
public sealed class UserProfileImageKeyEndToEndTests : IAsyncLifetime
{
    private readonly GmWebApplicationFactory<Program> _factory = new();
    private readonly string _userName = $"avatar-{Guid.NewGuid():N}";
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
                await context.Set<User>().IgnoreQueryFilters().Where(u => u.Id == _userId).ExecuteDeleteAsync();
            }
            catch { /* best-effort cleanup */ }
        }
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Profile_image_key_is_set_kept_replaced_and_cleared()
    {
        if (!_infraReady) return;

        _userId = await Send(new CreateUserCommand
        {
            Username = _userName,
            Email = $"{_userName}@test.local",
            Password = "Kx9!vR2#mQ7$wZ4p",
            ProfileImageKey = "avatars/first.png",
        });
        Assert.Equal("avatars/first.png", (await DetailsAsync()).ProfileImageKey);

        await Send(Update(profileImageKey: null));              // omitted → kept
        Assert.Equal("avatars/first.png", (await DetailsAsync()).ProfileImageKey);

        await Send(Update(profileImageKey: "avatars/second.png")); // new key → replaced
        Assert.Equal("avatars/second.png", (await DetailsAsync()).ProfileImageKey);

        await Send(Update(profileImageKey: ""));                // empty → cleared
        Assert.Null((await DetailsAsync()).ProfileImageKey);
    }

    private UpdateUserCommand Update(string? profileImageKey) => new()
    {
        Id = _userId,
        Username = _userName,
        Email = $"{_userName}@test.local",
        ProfileImageKey = profileImageKey,
    };

    private Task<UserDetailsDto> DetailsAsync() => Send(new GetUserDetailsQuery { Id = _userId });

    private async Task<TResult> Send<TResult>(IRequest<TResult> request)
    {
        using var scope = _factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IMediator>().Send(request);
    }

    private async Task Send(IRequest request)
    {
        using var scope = _factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IMediator>().Send(request);
    }
}
