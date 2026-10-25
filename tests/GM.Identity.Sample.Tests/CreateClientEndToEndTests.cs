using GM.Identity;
using GM.Identity.Sample.Application.Clients.Commands.CreateClient;
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
/// End-to-end proof of Create Client: the supplied secret is stored only as a hash, and the client's metadata
/// (redirect URIs, logout endpoints, consent flag) is persisted. Requires Postgres + Redis; no-ops otherwise.
/// </summary>
public sealed class CreateClientEndToEndTests : IAsyncLifetime
{
    private const string Secret = "create-client-secret";

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
    public async Task Creating_a_client_hashes_the_secret_and_persists_metadata()
    {
        if (!_infraReady) return;

        using (var scope = _factory.Services.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            _clientId = await mediator.Send(new CreateClientCommand
            {
                Name = $"create-{Guid.NewGuid():N}",
                Secret = Secret,
                RedirectUris = new[] { _redirect1, _redirect2, _redirect1 }, // duplicates are dropped
                BackchannelLogoutUri = "https://app.example/bcl",
                RequireConsent = true,
            });
        }

        Assert.NotEqual(Guid.Empty, _clientId);

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var client = await context.Set<Client>().IgnoreQueryFilters().FirstAsync(c => c.Id == _clientId);

            // The secret works, and it is stored only as a hash.
            Assert.True(PasswordHasher.Verify(Secret, client.SecretHash, client.SecretSalt));
            Assert.NotEqual(Secret, client.SecretHash);
            Assert.True(client.RequireConsent);
            Assert.Equal("https://app.example/bcl", client.BackchannelLogoutUri);

            var uris = await context.Set<ClientRedirectUri>().IgnoreQueryFilters()
                .Where(x => x.ClientId == _clientId).Select(x => x.Uri).ToListAsync();
            Assert.Equal(2, uris.Count);
            Assert.Contains(_redirect1, uris);
            Assert.Contains(_redirect2, uris);
        }
    }
}
