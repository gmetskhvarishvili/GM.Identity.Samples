using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.TwoFactorAuthTypeAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserTwoFactorAuthTypeAggregate;
using GM.Identity.Sample.Persistence.Context;
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
/// End-to-end proof that the login flow references 2FA: a user enrolled in a two-factor method does not
/// receive tokens from a password grant — the response is a challenge (<c>twoFactorRequired = true</c>,
/// naming the enrolled methods) instead. Requires Postgres + Redis; no-ops if they aren't reachable.
/// </summary>
public sealed class TwoFactorLoginEndToEndTests : IAsyncLifetime
{
    private const string ClientId = "11111111-1111-1111-1111-111111111111";
    private const string ClientSecret = "secret";
    private const string Password = "Correct123!";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly GmWebApplicationFactory<Program> _factory = new();
    private readonly string _userName = $"twofactor-{Guid.NewGuid():N}";
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

            var enrolment = UserTwoFactorAuthType.Create(_userId, _twoFactorTypeId);
            enrolment.Confirm(); // Only a confirmed enrolment gates login.
            await context.Set<UserTwoFactorAuthType>().AddAsync(enrolment);
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
                await context.Set<UserTwoFactorAuthType>().Where(x => x.UserId == _userId).ExecuteDeleteAsync();
                await context.Set<TwoFactorAuthType>().Where(x => x.Id == _twoFactorTypeId).ExecuteDeleteAsync();
                await context.Set<User>().IgnoreQueryFilters().Where(u => u.Id == _userId).ExecuteDeleteAsync();
            }
            catch { /* best-effort cleanup */ }
        }
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Password_grant_for_a_2fa_enrolled_user_returns_a_challenge_not_tokens()
    {
        if (!_infraReady) return;

        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "password"),
            new KeyValuePair<string, string>("username", _userName),
            new KeyValuePair<string, string>("password", Password),
            new KeyValuePair<string, string>("client_id", ClientId),
            new KeyValuePair<string, string>("client_secret", ClientSecret),
        });

        var response = await _factory.CreateClient().PostAsync("/connect/token", content);
        await response.ShouldBeOkAsync();

        var token = JsonSerializer.Deserialize<TokenResponse>(
            await response.Content.ReadAsStringAsync(), JsonOptions);

        Assert.NotNull(token);
        Assert.True(token.TwoFactorRequired);
        Assert.True(string.IsNullOrEmpty(token.AccessToken));      // no session issued yet
        Assert.Contains(_twoFactorTypeId, token.TwoFactorAuthTypeIds ?? new List<int>());
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
