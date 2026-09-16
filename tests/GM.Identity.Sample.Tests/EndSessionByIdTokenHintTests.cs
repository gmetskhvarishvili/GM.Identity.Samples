using GM.Identity;
using GM.Identity.Sample.Application.Accounts.Commands.EndSession;
using GM.Identity.Sample.Application.Infrastructure.Services.Logout;
using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.SsoSessionAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.UserSessionAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserAggregate;
using GM.Identity.Sample.Persistence.Context;
using GM.Mediator.Contracts;
using GM.Testing.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Tests;

/// <summary>
/// End-to-end proof that <c>/connect/endsession</c> can identify the session to end from a signed
/// <c>id_token_hint</c> when there is no SSO cookie — even an expired hint — and that a tampered/unknown hint
/// ends nothing. Requires Postgres + Redis; no-ops otherwise.
/// </summary>
public sealed class EndSessionByIdTokenHintTests : IAsyncLifetime
{
    private const string Issuer = "https://op.test";

    private readonly GmWebApplicationFactory<Program> _factory = new();
    private readonly string _userName = $"hint-{Guid.NewGuid():N}";

    private Guid _userId;
    private Guid _ssoSessionId;
    private string _idTokenHint = null!;
    private bool _infraReady;

    public async Task InitializeAsync()
    {
        try
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var user = User.Create(_userName, $"{_userName}@test.local", null);
            await context.Set<User>().AddAsync(user);

            var clientId = Guid.NewGuid();
            var sso = SsoSession.Create(
                user.Id, TokenGenerator.Hash(TokenGenerator.Generate()), DateTime.UtcNow, DateTime.UtcNow.AddHours(8));
            await context.Set<SsoSession>().AddAsync(sso);

            var session = UserSession.Create(
                user.Id, clientId, "test", TokenGenerator.Hash(TokenGenerator.Generate()),
                DateTime.UtcNow.AddDays(1), TokenGenerator.Hash(TokenGenerator.Generate()), sso.Id);
            await context.Set<UserSession>().AddAsync(session);
            await context.SaveChangesAsync();

            _userId = user.Id;
            _ssoSessionId = sso.Id;

            // An ALREADY-EXPIRED id_token whose sid names the SSO session (as a real RP would present at logout).
            var idTokenGenerator = scope.ServiceProvider.GetRequiredService<IIdTokenGenerator>();
            _idTokenHint = idTokenGenerator.Generate(new IdTokenParameters(
                Issuer, clientId, user.Id, SessionId: sso.Id,
                AuthTime: DateTime.UtcNow.AddMinutes(-30), ExpiresAt: DateTime.UtcNow.AddMinutes(-15),
                Email: null, EmailVerified: false, Name: _userName, Nonce: null));

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
                await context.Set<User>().IgnoreQueryFilters().Where(u => u.Id == _userId).ExecuteDeleteAsync();
            }
            catch { /* best-effort cleanup */ }
        }
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task An_expired_id_token_hint_ends_the_named_session()
    {
        if (!_infraReady) return;

        var result = await EndSessionAsync(_idTokenHint);
        Assert.Equal(1, result.RevokedSessions);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var sso = await context.Set<SsoSession>().IgnoreQueryFilters().FirstAsync(s => s.Id == _ssoSessionId);
        Assert.True(sso.IsRevoked);
    }

    [Fact]
    public async Task A_tampered_hint_ends_nothing()
    {
        if (!_infraReady) return;

        // Flip the signature segment — the OP must not trust it.
        var result = await EndSessionAsync(_idTokenHint + "tampered");
        Assert.Equal(0, result.RevokedSessions);
    }

    private async Task<EndSessionResponseDto> EndSessionAsync(string idTokenHint)
    {
        using var scope = _factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        return await mediator.Send(new EndSessionCommand { IdTokenHint = idTokenHint, Issuer = Issuer });
    }
}
