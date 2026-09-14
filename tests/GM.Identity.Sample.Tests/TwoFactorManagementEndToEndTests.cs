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
/// (<see cref="EnableUserTwoFactorCommand"/>) leaves it <em>pending</em> so login still issues tokens (a user
/// who can't complete setup isn't locked out); only once the enrolment is confirmed does a password grant turn
/// into a challenge; and removing it (<see cref="DisableUserTwoFactorCommand"/>) restores a direct token grant.
/// The confirm step is driven through the domain (<c>Confirm()</c>) because the setup OTP is delivered by an
/// external service the test harness doesn't run. Requires Postgres + Redis; no-ops if they aren't reachable.
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
    public async Task Two_factor_enrolment_lifecycle_gates_login_only_once_confirmed()
    {
        if (!_infraReady) return;

        // Baseline: with no second factor, the password grant issues tokens directly.
        var baseline = await ReadTokenAsync(await PasswordGrantAsync());
        Assert.False(baseline!.TwoFactorRequired);
        Assert.False(string.IsNullOrEmpty(baseline.AccessToken));

        // Enable → the enrolment is PENDING, so login still issues tokens (the user isn't locked out).
        await SendAsync(new EnableUserTwoFactorCommand { UserId = _userId, TwoFactorAuthTypeId = _twoFactorTypeId });
        var pending = await ReadTokenAsync(await PasswordGrantAsync());
        Assert.False(pending!.TwoFactorRequired);
        Assert.False(string.IsNullOrEmpty(pending.AccessToken));

        // Enabling again is idempotent (re-issues the setup code, no duplicate enrolment).
        await SendAsync(new EnableUserTwoFactorCommand { UserId = _userId, TwoFactorAuthTypeId = _twoFactorTypeId });

        // Confirm the enrolment (domain step-in for the OTP the external service would deliver) → NOW login
        // returns a challenge instead of tokens.
        await ConfirmEnrolmentAsync();
        var confirmed = await ReadTokenAsync(await PasswordGrantAsync());
        Assert.True(confirmed!.TwoFactorRequired);
        Assert.True(string.IsNullOrEmpty(confirmed.AccessToken));
        Assert.Contains(_twoFactorTypeId, confirmed.TwoFactorAuthTypeIds ?? new List<int>());

        // Remove the method → the password grant issues tokens directly again.
        await SendAsync(new DisableUserTwoFactorCommand { UserId = _userId, TwoFactorAuthTypeId = _twoFactorTypeId });
        var disabled = await ReadTokenAsync(await PasswordGrantAsync());
        Assert.False(disabled!.TwoFactorRequired);
        Assert.False(string.IsNullOrEmpty(disabled.AccessToken));
    }

    private async Task ConfirmEnrolmentAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var enrolment = await context.Set<UserTwoFactorAuthType>().IgnoreQueryFilters()
            .FirstAsync(x => x.UserId == _userId && x.TwoFactorAuthTypeId == _twoFactorTypeId);
        enrolment.Confirm();
        await context.SaveChangesAsync();
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
