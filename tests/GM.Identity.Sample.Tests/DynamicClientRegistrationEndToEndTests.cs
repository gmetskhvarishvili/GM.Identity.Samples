using GM.Identity;
using GM.Identity.Sample.Application.Clients.Commands.RegisterClient;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.ClientAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.ClientRedirectUriAggregate;
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
/// End-to-end proof of Dynamic Client Registration (RFC 7591): registering a client returns a working
/// client_id and a one-time client_secret (stored only hashed) and persists the supplied metadata (redirect
/// URIs, logout endpoints, consent flag). Requires Postgres + Redis; no-ops otherwise.
/// </summary>
public sealed class DynamicClientRegistrationEndToEndTests : IAsyncLifetime
{
    private readonly GmWebApplicationFactory<Program> _factory = new();
    private readonly string _redirect1 = $"https://app.example/cb/{Guid.NewGuid():N}";
    private readonly string _redirect2 = $"https://app.example/cb2/{Guid.NewGuid():N}";
    private Guid _clientId;
    private bool _infraReady;

    public Task InitializeAsync()
    {
        try
        {
            using var scope = _factory.Services.CreateScope();
            _ = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            _infraReady = true;
        }
        catch
        {
            _infraReady = false;
        }
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (_infraReady && _clientId != Guid.Empty)
        {
            try
            {
                using var scope = _factory.Services.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await context.Set<ClientRedirectUri>().IgnoreQueryFilters().Where(x => x.ClientId == _clientId).ExecuteDeleteAsync();
                await context.Set<Client>().IgnoreQueryFilters().Where(c => c.Id == _clientId).ExecuteDeleteAsync();
            }
            catch { /* best-effort cleanup */ }
        }
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Registering_a_client_returns_working_credentials_and_persists_metadata()
    {
        if (!_infraReady) return;

        RegisterClientResponseDto response;
        using (var scope = _factory.Services.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            response = await mediator.Send(new RegisterClientCommand
            {
                ClientName = $"dcr-{Guid.NewGuid():N}",
                RedirectUris = new[] { _redirect1, _redirect2 },
                Scope = "openid email",
                BackchannelLogoutUri = "https://app.example/bcl",
                RequireConsent = true,
            });
        }

        _clientId = response.ClientId;
        Assert.NotEqual(Guid.Empty, response.ClientId);
        Assert.False(string.IsNullOrWhiteSpace(response.ClientSecret));
        Assert.Equal(0, response.ClientSecretExpiresAt);
        Assert.Equal(2, response.RedirectUris.Count);

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var client = await context.Set<Client>().IgnoreQueryFilters().FirstAsync(c => c.Id == _clientId);

            // The returned secret is the real one — and it is stored only as a hash.
            Assert.True(PasswordHasher.Verify(response.ClientSecret, client.SecretHash, client.SecretSalt));
            Assert.NotEqual(response.ClientSecret, client.SecretHash);
            Assert.True(client.RequireConsent);
            Assert.Equal("https://app.example/bcl", client.BackchannelLogoutUri);

            var uris = await context.Set<ClientRedirectUri>().IgnoreQueryFilters()
                .Where(x => x.ClientId == _clientId).Select(x => x.Uri).ToListAsync();
            Assert.Contains(_redirect1, uris);
            Assert.Contains(_redirect2, uris);
        }
    }
}
