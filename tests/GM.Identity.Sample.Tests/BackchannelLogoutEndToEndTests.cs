using GM.Identity;
using GM.Identity.Sample.Application.Accounts.Commands.EndSession;
using GM.Identity.Sample.Application.Infrastructure.Services.Logout;
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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Tests;

/// <summary>
/// End-to-end proof that Single Logout drives OIDC back-channel logout: ending the SSO session notifies every
/// relying party that registered a back-channel logout endpoint, naming the subject (user) and session (sid)
/// that ended. A capturing notifier stands in for the network so the wiring is asserted deterministically.
/// Requires Postgres + Redis; no-ops otherwise.
/// </summary>
public sealed class BackchannelLogoutEndToEndTests : IAsyncLifetime
{
    private const string LogoutUri = "https://rp.example/backchannel-logout";

    private readonly CapturingNotifier _notifier = new();
    private readonly GmWebApplicationFactory<Program> _factory;
    private readonly string _userName = $"bcl-{Guid.NewGuid():N}";

    private Guid _userId;
    private Guid _clientId;
    private Guid _ssoSessionId;
    private string _ssoCookie = null!;
    private bool _infraReady;

    public BackchannelLogoutEndToEndTests() =>
        _factory = new GmWebApplicationFactory<Program>().ReplaceService<IBackchannelLogoutNotifier>(_notifier);

    public async Task InitializeAsync()
    {
        try
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var user = User.Create(_userName, $"{_userName}@test.local", null);
            await context.Set<User>().AddAsync(user);

            // A dedicated client that participates in back-channel logout.
            var client = Client.Create($"bcl-client-{Guid.NewGuid():N}");
            var (secretHash, secretSalt) = PasswordHasher.Hash("secret");
            client.UpdateSecret(secretHash, secretSalt);
            client.SetBackchannelLogoutUri(LogoutUri);
            await context.Set<Client>().AddAsync(client);

            _ssoCookie = TokenGenerator.Generate();
            var sso = SsoSession.Create(
                user.Id, TokenGenerator.Hash(_ssoCookie), DateTime.UtcNow, DateTime.UtcNow.AddHours(8));
            await context.Set<SsoSession>().AddAsync(sso);

            // An app session established through the SSO session (carries its id → the SLO link).
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
    public async Task Logout_notifies_the_relying_party_with_the_subject_and_session()
    {
        if (!_infraReady) return;

        EndSessionResponseDto result;
        using (var scope = _factory.Services.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            result = await mediator.Send(new EndSessionCommand { SsoCookie = _ssoCookie, Issuer = "https://op.test" });
        }

        Assert.Equal(1, result.RevokedSessions);

        var target = Assert.Single(_notifier.Targets);
        Assert.Equal(_clientId, target.ClientId);
        Assert.Equal(LogoutUri, target.LogoutUri);
        Assert.Equal(_userId, target.UserId);
        Assert.Equal(_ssoSessionId, target.SessionId);
        Assert.Equal("https://op.test", _notifier.Issuer);
    }

    private sealed class CapturingNotifier : IBackchannelLogoutNotifier
    {
        public string? Issuer { get; private set; }
        public List<BackchannelLogoutTarget> Targets { get; } = new();

        public Task NotifyAsync(
            string issuer, IReadOnlyCollection<BackchannelLogoutTarget> targets, CancellationToken cancellationToken)
        {
            Issuer = issuer;
            Targets.AddRange(targets);
            return Task.CompletedTask;
        }
    }
}
