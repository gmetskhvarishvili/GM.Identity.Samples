using GM.Identity;
using GM.Identity.Authorization;
using GM.Identity.Sample.Application.Accounts.Commands.Authorize;
using GM.Identity.Sample.Application.Accounts.Commands.AuthorizeCode;
using GM.Identity.Sample.Application.Accounts.Commands.EndSession;
using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.AuthorizationCodeAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.SsoSessionAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.UserSessionAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.ClientRedirectUriAggregate;
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

namespace GM.Identity.Sample.Tests;

/// <summary>
/// End-to-end proof of cross-application single sign-on. One interactive login at <c>/connect/authorize</c>
/// establishes an SSO session (cookie); a second application then obtains an authorization code silently — with
/// no credentials — by presenting that cookie. Both app sessions are linked to the SSO session, so a single
/// <c>/connect/endsession</c> logs the user out of every application at once (Single Logout) and evicts their
/// tokens from the session cache. Afterwards, silent authorization (<c>prompt=none</c>) is refused.
/// Requires Postgres + Redis; no-ops if they aren't reachable.
/// </summary>
public sealed class SsoEndToEndTests : IAsyncLifetime
{
    private const string ClientId = "11111111-1111-1111-1111-111111111111";
    private const string ClientSecret = "secret";
    private const string Password = "Correct123!";

    private readonly GmWebApplicationFactory<Program> _factory = new();
    private readonly string _userName = $"sso-{Guid.NewGuid():N}";
    private readonly string _app1Redirect = $"https://app1.example/cb/{Guid.NewGuid():N}";
    private readonly string _app2Redirect = $"https://app2.example/cb/{Guid.NewGuid():N}";
    private readonly Guid _clientId = Guid.Parse(ClientId);
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

            // Two applications, modelled as two registered redirect URIs of the same client.
            await context.Set<ClientRedirectUri>().AddAsync(ClientRedirectUri.Create(_clientId, _app1Redirect));
            await context.Set<ClientRedirectUri>().AddAsync(ClientRedirectUri.Create(_clientId, _app2Redirect));
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
                await context.Set<UserSession>().Where(s => s.UserId == _userId).ExecuteDeleteAsync();
                await context.Set<SsoSession>().IgnoreQueryFilters().Where(s => s.UserId == _userId).ExecuteDeleteAsync();
                await context.Set<AuthorizationCode>().IgnoreQueryFilters()
                    .Where(a => a.UserId == _userId).ExecuteDeleteAsync();
                await context.Set<ClientRedirectUri>().IgnoreQueryFilters()
                    .Where(x => x.ClientId == _clientId && (x.Uri == _app1Redirect || x.Uri == _app2Redirect))
                    .ExecuteDeleteAsync();
                await context.Set<User>().IgnoreQueryFilters().Where(u => u.Id == _userId).ExecuteDeleteAsync();
            }
            catch { /* best-effort cleanup */ }
        }
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task One_login_authorizes_a_second_app_silently_and_logout_ends_every_session()
    {
        if (!_infraReady) return;

        // 1) Interactive login for app 1 → an authorization code AND a new SSO session cookie.
        var verifier1 = PkceHelper.GenerateCodeVerifier();
        var authorize1 = await AuthorizeCodeAsync(new AuthorizeCodeCommand
        {
            ClientId = _clientId,
            RedirectUri = _app1Redirect,
            CodeChallenge = PkceHelper.GenerateCodeChallenge(verifier1),
            UserName = _userName,
            Password = Password,
        });
        Assert.False(string.IsNullOrWhiteSpace(authorize1.Code));
        Assert.False(string.IsNullOrEmpty(authorize1.SsoCookie)); // a fresh SSO session was established
        var ssoCookie = authorize1.SsoCookie!;

        var tokens1 = await ExchangeAsync(authorize1.Code, verifier1, _app1Redirect);
        Assert.False(string.IsNullOrEmpty(tokens1.AccessToken));

        // 2) App 2 authorizes SILENTLY — SSO cookie only, no username/password.
        var verifier2 = PkceHelper.GenerateCodeVerifier();
        var authorize2 = await AuthorizeCodeAsync(new AuthorizeCodeCommand
        {
            ClientId = _clientId,
            RedirectUri = _app2Redirect,
            CodeChallenge = PkceHelper.GenerateCodeChallenge(verifier2),
            SsoCookie = ssoCookie,
        });
        Assert.False(string.IsNullOrWhiteSpace(authorize2.Code));
        Assert.True(string.IsNullOrEmpty(authorize2.SsoCookie)); // reused the existing session; no new cookie

        var tokens2 = await ExchangeAsync(authorize2.Code, verifier2, _app2Redirect);
        Assert.False(string.IsNullOrEmpty(tokens2.AccessToken));

        // Both app sessions are linked to the one SSO session.
        Guid ssoSessionId;
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var sessions = await context.Set<UserSession>()
                .Where(s => s.UserId == _userId).ToListAsync();
            Assert.Equal(2, sessions.Count);
            Assert.All(sessions, s => Assert.NotNull(s.SsoSessionId));
            ssoSessionId = sessions.Select(s => s.SsoSessionId!.Value).Distinct().Single();
        }

        // 3) Single Logout: one endsession call revokes both app sessions.
        var logout = await EndSessionAsync(ssoCookie);
        Assert.Equal(2, logout.RevokedSessions);

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var sessions = await context.Set<UserSession>().Where(s => s.UserId == _userId).ToListAsync();
            Assert.All(sessions, s => Assert.True(s.IsRevoked));

            var sso = await context.Set<SsoSession>().IgnoreQueryFilters()
                .FirstOrDefaultAsync(s => s.Id == ssoSessionId);
            Assert.NotNull(sso);
            Assert.True(sso!.IsRevoked);

            // Tokens are gone from the session cache — bearer validation will now fail everywhere.
            var cache = scope.ServiceProvider.GetRequiredService<ISessionCache>();
            var cachedHashes = await cache.GetCachedTokenHashesAsync();
            Assert.DoesNotContain(TokenGenerator.Hash(tokens1.AccessToken!), cachedHashes);
            Assert.DoesNotContain(TokenGenerator.Hash(tokens2.AccessToken!), cachedHashes);
        }

        // 4) The SSO session is dead — silent authorization is refused.
        await Assert.ThrowsAnyAsync<Exception>(() => AuthorizeCodeAsync(new AuthorizeCodeCommand
        {
            ClientId = _clientId,
            RedirectUri = _app1Redirect,
            CodeChallenge = PkceHelper.GenerateCodeChallenge(PkceHelper.GenerateCodeVerifier()),
            SsoCookie = ssoCookie,
            Prompt = "none",
        }));
    }

    [Fact]
    public async Task Silent_authorization_without_an_sso_session_is_refused()
    {
        if (!_infraReady) return;

        // prompt=none with no SSO cookie must fail rather than prompt for credentials (OIDC login_required).
        await Assert.ThrowsAnyAsync<Exception>(() => AuthorizeCodeAsync(new AuthorizeCodeCommand
        {
            ClientId = _clientId,
            RedirectUri = _app1Redirect,
            CodeChallenge = PkceHelper.GenerateCodeChallenge(PkceHelper.GenerateCodeVerifier()),
            Prompt = "none",
        }));
    }

    private async Task<AuthorizeCodeResponseDto> AuthorizeCodeAsync(AuthorizeCodeCommand command)
    {
        using var scope = _factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        return await mediator.Send(command);
    }

    private async Task<AuthorizeResponseDto> ExchangeAsync(string code, string verifier, string redirectUri)
    {
        using var scope = _factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        return await mediator.Send(new AuthorizeCommand
        {
            GrantType = "authorization_code",
            Code = code,
            RedirectUri = redirectUri,
            CodeVerifier = verifier,
            ClientId = _clientId,
            ClientSecret = ClientSecret,
        });
    }

    private async Task<EndSessionResponseDto> EndSessionAsync(string ssoCookie)
    {
        using var scope = _factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        return await mediator.Send(new EndSessionCommand { SsoCookie = ssoCookie });
    }
}
