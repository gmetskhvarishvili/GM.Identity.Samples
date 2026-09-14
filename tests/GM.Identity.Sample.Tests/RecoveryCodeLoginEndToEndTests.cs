using GM.Identity.Sample.Application.Users.Commands.GenerateRecoveryCodes;
using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.UserSessionAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.TwoFactorAuthTypeAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserRecoveryCodeAggregate;
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
/// End-to-end proof that a single-use backup code stands in for the normal second factor: a 2FA-enrolled user
/// can complete a <c>grant_type=two_factor</c> login by presenting a recovery code instead of the OTP, and that
/// code is then spent (a replay is rejected). Requires Postgres + Redis; no-ops if they aren't reachable.
/// </summary>
public sealed class RecoveryCodeLoginEndToEndTests : IAsyncLifetime
{
    private const string ClientId = "11111111-1111-1111-1111-111111111111";
    private const string ClientSecret = "secret";
    private const string Password = "Correct123!";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly GmWebApplicationFactory<Program> _factory = new();
    private readonly string _userName = $"recovery-{Guid.NewGuid():N}";
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
            enrolment.Confirm(); // Confirmed → login requires a second factor.
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
                await context.Set<UserRecoveryCode>().IgnoreQueryFilters().Where(x => x.UserId == _userId).ExecuteDeleteAsync();
                await context.Set<UserSession>().Where(s => s.UserId == _userId).ExecuteDeleteAsync();
                await context.Set<UserTwoFactorAuthType>().IgnoreQueryFilters().Where(x => x.UserId == _userId).ExecuteDeleteAsync();
                await context.Set<TwoFactorAuthType>().Where(x => x.Id == _twoFactorTypeId).ExecuteDeleteAsync();
                await context.Set<User>().IgnoreQueryFilters().Where(u => u.Id == _userId).ExecuteDeleteAsync();
            }
            catch { /* best-effort cleanup */ }
        }
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task A_recovery_code_completes_a_two_factor_login_and_is_then_spent()
    {
        if (!_infraReady) return;

        // Generate the backup codes (returned in plaintext exactly once).
        IReadOnlyList<string> codes;
        using (var scope = _factory.Services.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            codes = await mediator.Send(new GenerateRecoveryCodesCommand { UserId = _userId });
        }
        Assert.NotEmpty(codes);
        var recoveryCode = codes[0];

        // Step 1: password grant returns a challenge (the user has a confirmed second factor).
        var challenge = await ReadTokenAsync(await PasswordGrantAsync());
        Assert.True(challenge!.TwoFactorRequired);

        // Step 2: complete the login by presenting a recovery code in place of the OTP.
        var completed = await TokenAsync(
            ("grant_type", "two_factor"), ("username", _userName), ("password", Password),
            ("code", recoveryCode), ("client_id", ClientId), ("client_secret", ClientSecret));
        await completed.ShouldBeOkAsync();
        var tokens = await ReadTokenAsync(completed);
        Assert.False(string.IsNullOrEmpty(tokens!.AccessToken));

        // The code is single-use: replaying it fails.
        var replay = await TokenAsync(
            ("grant_type", "two_factor"), ("username", _userName), ("password", Password),
            ("code", recoveryCode), ("client_id", ClientId), ("client_secret", ClientSecret));
        Assert.False(replay.IsSuccessStatusCode);
    }

    private Task<HttpResponseMessage> PasswordGrantAsync() =>
        TokenAsync(
            ("grant_type", "password"), ("username", _userName), ("password", Password),
            ("client_id", ClientId), ("client_secret", ClientSecret));

    private Task<HttpResponseMessage> TokenAsync(params (string Key, string Value)[] fields)
    {
        var content = new FormUrlEncodedContent(
            fields.Select(f => new KeyValuePair<string, string>(f.Key, f.Value)));
        return _factory.CreateClient().PostAsync("/connect/token", content);
    }

    private static async Task<TokenResponse?> ReadTokenAsync(HttpResponseMessage response) =>
        JsonSerializer.Deserialize<TokenResponse>(
            await response.Content.ReadAsStringAsync(), JsonOptions);

    private sealed record TokenResponse(
        string? AccessToken,
        DateTime? ExpiresAt,
        string TokenType,
        string? RefreshToken,
        DateTime? RefreshTokenExpiresAt,
        bool TwoFactorRequired,
        List<int>? TwoFactorAuthTypeIds);
}
