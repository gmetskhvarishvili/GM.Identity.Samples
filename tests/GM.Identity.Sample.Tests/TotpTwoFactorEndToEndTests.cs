using GM.Identity.Sample.Application.Common.Security;
using GM.Identity.Sample.Application.Users.Commands.ConfirmUserTotp;
using GM.Identity.Sample.Application.Users.Commands.SetupUserTotp;
using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.UserSessionAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserTotpDeviceAggregate;
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
/// End-to-end proof of authenticator-app (TOTP) 2FA: after setup + confirm, a password grant returns a
/// challenge and the login completes with a code computed from the shared secret (RFC 6238). A wrong code is
/// rejected. Requires Postgres + Redis; no-ops if they aren't reachable.
/// </summary>
public sealed class TotpTwoFactorEndToEndTests : IAsyncLifetime
{
    private const string ClientId = "11111111-1111-1111-1111-111111111111";
    private const string ClientSecret = "secret";
    private const string Password = "Correct123!";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly GmWebApplicationFactory<Program> _factory = new();
    private readonly string _userName = $"totp-{Guid.NewGuid():N}";
    private Guid _userId;
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
                await context.Set<UserTotpDevice>().IgnoreQueryFilters().Where(x => x.UserId == _userId).ExecuteDeleteAsync();
                await context.Set<UserSession>().Where(s => s.UserId == _userId).ExecuteDeleteAsync();
                await context.Set<User>().IgnoreQueryFilters().Where(u => u.Id == _userId).ExecuteDeleteAsync();
            }
            catch { /* best-effort cleanup */ }
        }
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Totp_gates_login_after_confirmation_and_completes_with_a_generated_code()
    {
        if (!_infraReady) return;

        // Setup → get the shared secret (its own scope, as a real request would be).
        string secret;
        using (var scope = _factory.Services.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var setup = await mediator.Send(new SetupUserTotpCommand { UserId = _userId });
            secret = setup.Secret;
            Assert.StartsWith("otpauth://totp/", setup.OtpauthUri);
        }

        // Confirm with a generated code (activates the device) — separate scope / DbContext.
        using (var scope = _factory.Services.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            await mediator.Send(new ConfirmUserTotpCommand
            {
                UserId = _userId,
                Code = Totp.ComputeForTest(secret, DateTimeOffset.UtcNow),
            });
        }

        // Password grant now returns a challenge (no tokens yet).
        var challenge = await ReadTokenAsync(await PasswordGrantAsync());
        Assert.True(challenge!.TwoFactorRequired);
        Assert.True(string.IsNullOrEmpty(challenge.AccessToken));

        // Complete the login with a fresh TOTP code.
        var completed = await TokenAsync(
            ("grant_type", "two_factor"), ("username", _userName), ("password", Password),
            ("code", Totp.ComputeForTest(secret, DateTimeOffset.UtcNow)),
            ("client_id", ClientId), ("client_secret", ClientSecret));
        await completed.ShouldBeOkAsync();
        var tokens = await ReadTokenAsync(completed);
        Assert.False(string.IsNullOrEmpty(tokens!.AccessToken));

        // A wrong code is rejected.
        var wrong = await TokenAsync(
            ("grant_type", "two_factor"), ("username", _userName), ("password", Password),
            ("code", "000000"), ("client_id", ClientId), ("client_secret", ClientSecret));
        Assert.False(wrong.IsSuccessStatusCode);
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
        JsonSerializer.Deserialize<TokenResponse>(await response.Content.ReadAsStringAsync(), JsonOptions);

    private sealed record TokenResponse(
        string? AccessToken,
        DateTime? ExpiresAt,
        string TokenType,
        string? RefreshToken,
        DateTime? RefreshTokenExpiresAt,
        bool TwoFactorRequired,
        List<int>? TwoFactorAuthTypeIds);
}
