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
/// End-to-end proof of the user's profile image file id: set at creation, returned in the details, kept by an update
/// that omits it, replaced by one that sends a new id, and cleared by <see cref="Guid.Empty"/>. Requires Postgres + Redis;
/// no-ops if they aren't reachable.
/// </summary>
public sealed class UserProfileImageFileIdEndToEndTests : IAsyncLifetime
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
    public async Task Profile_image_file_id_is_set_kept_replaced_and_cleared()
    {
        if (!_infraReady) return;

        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        _userId = await Send(new CreateUserCommand
        {
            Username = _userName,
            Email = $"{_userName}@test.local",
            Password = "Kx9!vR2#mQ7$wZ4p",
            ProfileImageFileId = first,
        });
        Assert.Equal(first, (await DetailsAsync()).ProfileImageFileId);

        await Send(Update(profileImageFileId: null));       // omitted → kept
        Assert.Equal(first, (await DetailsAsync()).ProfileImageFileId);

        await Send(Update(profileImageFileId: second));     // new id → replaced
        Assert.Equal(second, (await DetailsAsync()).ProfileImageFileId);

        await Send(Update(profileImageFileId: Guid.Empty)); // empty id → cleared
        Assert.Null((await DetailsAsync()).ProfileImageFileId);
    }

    private UpdateUserCommand Update(Guid? profileImageFileId) => new()
    {
        Id = _userId,
        Username = _userName,
        Email = $"{_userName}@test.local",
        ProfileImageFileId = profileImageFileId,
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
