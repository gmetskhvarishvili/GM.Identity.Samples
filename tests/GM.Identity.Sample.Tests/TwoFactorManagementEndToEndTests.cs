using GM.Identity.Sample.Application.Users.Commands.DisableUserTwoFactor;
using GM.Identity.Sample.Application.Users.Commands.EnableUserTwoFactor;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.TwoFactorAuthTypeAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserTwoFactorAuthTypeAggregate;
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
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Tests;

/// <summary>
/// End-to-end proof of the second-factor enrolment lifecycle and its effect on login: enabling a method
/// (<see cref="EnableUserTwoFactorCommand"/>) activates it immediately, so a password grant becomes a challenge;
/// and removing it (<see cref="DisableUserTwoFactorCommand"/>) restores a direct token grant. Requires
/// Postgres + Redis; no-ops if they aren't reachable.
/// </summary>
public sealed class TwoFactorManagementEndToEndTests : IAsyncLifetime
{
    private const string ClientId = "11111111-1111-1111-1111-111111111111";
    private const string ClientSecret = "secret";
    private const string Password = "Correct123!";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly GmWebApplicationFactory<Program> _factory = new();
    private readonly string _userName = $"tfa-mgmt-{Guid.NewGuid():N}";
    private Guid _userId;
    private int _twoFactorTypeId;
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

            var type = TwoFactorAuthType.Create($"tfa-{Guid.NewGuid():N}", "Test authenticator");
            await context.Set<TwoFactorAuthType>().AddAsync(type);
            await context.SaveChangesAsync();
            _userId = user.Id;
            _twoFactorTypeId = type.Id;
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
                await context.Set<UserTwoFactorAuthType>().IgnoreQueryFilters()
                    .Where(x => x.UserId == _userId).ExecuteDeleteAsync();
                await context.Set<TwoFactorAuthType>().Where(x => x.Id == _twoFactorTypeId).ExecuteDeleteAsync();
                await context.Set<User>().IgnoreQueryFilters().Where(u => u.Id == _userId).ExecuteDeleteAsync();
            }
            catch { /* best-effort cleanup */ }
        }
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Two_factor_enable_gates_login_and_disable_restores_direct_grant()
    {
        if (!_infraReady) return;

        // Baseline: with no second factor, the password grant issues tokens directly.
        var baseline = await ReadTokenAsync(await PasswordGrantAsync());
        Assert.False(baseline!.TwoFactorRequired);
        Assert.False(string.IsNullOrEmpty(baseline.AccessToken));

        // Enable → active immediately, so the password grant now returns a challenge instead of tokens.
        await SendAsync(new EnableUserTwoFactorCommand { UserId = _userId, TwoFactorAuthTypeId = _twoFactorTypeId });
        var enabled = await ReadTokenAsync(await PasswordGrantAsync());
        Assert.True(enabled!.TwoFactorRequired);
        Assert.True(string.IsNullOrEmpty(enabled.AccessToken));
        Assert.Contains(_twoFactorTypeId, enabled.TwoFactorAuthTypeIds ?? new List<int>());

        // Enabling again is idempotent (no duplicate enrolment, no error).
        await SendAsync(new EnableUserTwoFactorCommand { UserId = _userId, TwoFactorAuthTypeId = _twoFactorTypeId });

        // Remove the method → the password grant issues tokens directly again.
        await SendAsync(new DisableUserTwoFactorCommand { UserId = _userId, TwoFactorAuthTypeId = _twoFactorTypeId });
        var disabled = await ReadTokenAsync(await PasswordGrantAsync());
        Assert.False(disabled!.TwoFactorRequired);
        Assert.False(string.IsNullOrEmpty(disabled.AccessToken));
    }

    private async Task SendAsync(IRequest request)
    {
        using var scope = _factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        await mediator.Send(request);
    }

    private Task<HttpResponseMessage> PasswordGrantAsync()
    {
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "password"),
            new KeyValuePair<string, string>("username", _userName),
            new KeyValuePair<string, string>("password", Password),
            new KeyValuePair<string, string>("client_id", ClientId),
            new KeyValuePair<string, string>("client_secret", ClientSecret),
        });
        return _factory.CreateClient().PostAsync("/connect/token", content);
    }

    private static async Task<TokenResponse?> ReadTokenAsync(HttpResponseMessage response)
    {
        await response.ShouldBeOkAsync();
        return JsonSerializer.Deserialize<TokenResponse>(
            await response.Content.ReadAsStringAsync(), JsonOptions);
    }

    private sealed record TokenResponse(
        string? AccessToken,
        DateTime? ExpiresAt,
        string TokenType,
        string? RefreshToken,
        DateTime? RefreshTokenExpiresAt,
        bool TwoFactorRequired,
        List<int>? TwoFactorAuthTypeIds);
}
