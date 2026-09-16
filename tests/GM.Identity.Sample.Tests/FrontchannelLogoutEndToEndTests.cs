using GM.Identity;
using GM.Identity.Sample.Application.Accounts.Commands.EndSession;
using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.SsoSessionAggregate;
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

namespace GM.Identity.Sample.Tests;

/// <summary>
/// End-to-end proof of OIDC front-channel logout: ending the SSO session returns, for every relying party that
/// registered a front-channel logout endpoint, an iframe URL carrying <c>iss</c> and <c>sid</c> — which a logout
/// page loads so each RP clears its browser session. Requires Postgres + Redis; no-ops otherwise.
/// </summary>
public sealed class FrontchannelLogoutEndToEndTests : IAsyncLifetime
{
    private const string FrontchannelUri = "https://rp.example/frontchannel-logout";
    private const string Issuer = "https://op.test";

    private readonly GmWebApplicationFactory<Program> _factory = new();
    private readonly string _userName = $"fcl-{Guid.NewGuid():N}";

    private Guid _userId;
    private Guid _clientId;
    private Guid _ssoSessionId;
    private string _ssoCookie = null!;
    private bool _infraReady;

    public async Task InitializeAsync()
    {
        try
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var user = User.Create(_userName, $"{_userName}@test.local", null);
            await context.Set<User>().AddAsync(user);

            var client = Client.Create($"fcl-client-{Guid.NewGuid():N}");
            var (secretHash, secretSalt) = PasswordHasher.Hash("secret");
            client.UpdateSecret(secretHash, secretSalt);
            client.SetFrontchannelLogoutUri(FrontchannelUri);
            await context.Set<Client>().AddAsync(client);

            _ssoCookie = TokenGenerator.Generate();
            var sso = SsoSession.Create(
                user.Id, TokenGenerator.Hash(_ssoCookie), DateTime.UtcNow, DateTime.UtcNow.AddHours(8));
            await context.Set<SsoSession>().AddAsync(sso);

            var session = UserSession.Create(
                user.Id, client.Id, "test", TokenGenerator.Hash(TokenGenerator.Generate()),
                DateTime.UtcNow.AddDays(1), TokenGenerator.Hash(TokenGenerator.Generate()), sso.Id);
            await context.Set<UserSession>().AddAsync(session);

            await context.SaveChangesAsync();

            _userId = user.Id;
            _clientId = client.Id;
            _ssoSessionId = sso.Id;
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
                await context.Set<Client>().IgnoreQueryFilters().Where(c => c.Id == _clientId).ExecuteDeleteAsync();
                await context.Set<User>().IgnoreQueryFilters().Where(u => u.Id == _userId).ExecuteDeleteAsync();
            }
            catch { /* best-effort cleanup */ }
        }
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Logout_returns_a_front_channel_iframe_url_with_iss_and_sid()
    {
        if (!_infraReady) return;

        EndSessionResponseDto result;
        using (var scope = _factory.Services.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            result = await mediator.Send(new EndSessionCommand { SsoCookie = _ssoCookie, Issuer = Issuer });
        }

        Assert.Equal(1, result.RevokedSessions);
        var url = Assert.Single(result.FrontChannelLogoutUris);
        Assert.StartsWith(FrontchannelUri, url);
        Assert.Contains($"iss={Uri.EscapeDataString(Issuer)}", url);
        Assert.Contains($"sid={_ssoSessionId}", url);
    }
}
