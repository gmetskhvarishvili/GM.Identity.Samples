using GM.Exceptions;
using GM.Identity;
using GM.Identity.Sample.Application.Accounts.Commands.ApproveDevice;
using GM.Identity.Sample.Application.Accounts.Commands.Authorize;
using GM.Identity.Sample.Application.Accounts.Commands.DeviceAuthorization;
using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.DeviceCodeAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.UserSessionAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.ClientAggregate;
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
using ValidationException = GM.Exceptions.ValidationException;

namespace GM.Identity.Sample.Tests;

/// <summary>
/// End-to-end proof of the Device Authorization Grant (RFC 8628): a device requests a device_code + user_code,
/// polling returns authorization_pending (and slow_down when too fast) until the user approves, then a poll
/// yields tokens and the device_code is single-use. A denied request returns access_denied. Requires Postgres +
/// Redis; no-ops otherwise.
/// </summary>
public sealed class DeviceAuthorizationEndToEndTests : IAsyncLifetime
{
    private const string Password = "Correct123!";
    private const string Secret = "secret";
    private const string Issuer = "https://op.test";

    private readonly GmWebApplicationFactory<Program> _factory = new();
    private readonly string _userName = $"device-{Guid.NewGuid():N}";
    private Guid _userId;
    private Guid _clientId;
    private bool _infraReady;

    public async Task InitializeAsync()
    {
        try
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var user = User.Create(_userName, $"{_userName}@test.local", null);
            var (hash, salt) = PasswordHasher.Hash(Password);
            user.UpdatePassword(hash, salt);
            await context.Set<User>().AddAsync(user);

            var client = Client.Create($"device-client-{Guid.NewGuid():N}");
            var (secretHash, secretSalt) = PasswordHasher.Hash(Secret);
            client.UpdateSecret(secretHash, secretSalt);
            await context.Set<Client>().AddAsync(client);
            await context.SaveChangesAsync();

            _userId = user.Id;
            _clientId = client.Id;
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
                await context.Set<DeviceCode>().IgnoreQueryFilters().Where(d => d.ClientId == _clientId).ExecuteDeleteAsync();
                await context.Set<UserSession>().Where(s => s.UserId == _userId).ExecuteDeleteAsync();
                await context.Set<Client>().IgnoreQueryFilters().Where(c => c.Id == _clientId).ExecuteDeleteAsync();
                await context.Set<User>().IgnoreQueryFilters().Where(u => u.Id == _userId).ExecuteDeleteAsync();
            }
            catch { /* best-effort cleanup */ }
        }
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Poll_pends_then_slows_then_yields_tokens_after_approval_and_is_single_use()
    {
        if (!_infraReady) return;

        var device = await RequestDeviceAsync();
        Assert.False(string.IsNullOrWhiteSpace(device.DeviceCode));
        Assert.Contains("-", device.UserCode); // human-readable "XXXX-XXXX"

        // Before approval, polling is pending; an immediate second poll is throttled.
        Assert.Equal("authorization_pending", (await PollErrorAsync(device.DeviceCode)).Message);
        Assert.Equal("slow_down", (await PollErrorAsync(device.DeviceCode)).Message);

        // The user approves the user_code.
        await ApproveAsync(device.UserCode, approve: true);

        // Now a poll yields tokens…
        var tokens = await PollAsync(device.DeviceCode);
        Assert.False(string.IsNullOrEmpty(tokens.AccessToken));

        // …and the device_code is single-use.
        Assert.Equal("expired_token", (await PollErrorAsync(device.DeviceCode)).Message);
    }

    [Fact]
    public async Task A_denied_request_returns_access_denied()
    {
        if (!_infraReady) return;

        var device = await RequestDeviceAsync();
        await ApproveAsync(device.UserCode, approve: false);

        Assert.Equal("access_denied", (await PollErrorAsync(device.DeviceCode)).Message);
    }

    private async Task<DeviceAuthorizationResponseDto> RequestDeviceAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        return await mediator.Send(new DeviceAuthorizationCommand
        {
            ClientId = _clientId,
            ClientSecret = Secret,
            Issuer = Issuer,
        });
    }

    private async Task ApproveAsync(string userCode, bool approve)
    {
        using var scope = _factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        await mediator.Send(new ApproveDeviceCommand
        {
            UserCode = userCode,
            UserName = _userName,
            Password = Password,
            Approve = approve,
        });
    }

    private async Task<AuthorizeResponseDto> PollAsync(string deviceCode)
    {
        using var scope = _factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        return await mediator.Send(new AuthorizeCommand
        {
            GrantType = "urn:ietf:params:oauth:grant-type:device_code",
            DeviceCode = deviceCode,
            ClientId = _clientId,
            ClientSecret = Secret,
            Issuer = Issuer,
        });
    }

    private async Task<ValidationException> PollErrorAsync(string deviceCode) =>
        await Assert.ThrowsAsync<ValidationException>(() => PollAsync(deviceCode));
}
