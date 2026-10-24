using GM.Identity.Sample.Application.Clients.Commands.CreateClient;
using GM.Identity.Sample.Application.Clients.Commands.RotateClientSecret;
using GM.Identity.Sample.Application.Clients.Commands.UpdateClient;
using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.ClientSessionAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.ClientAggregate;
using GM.Identity.Sample.Persistence.Context;
using GM.Mediator.Contracts;
using GM.Testing.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Tests;

/// <summary>
/// A client's secret is changed only through Rotate Client Secret: the supplied secret replaces the old one (the old
/// one stops working, the new one signs in) and the sessions issued under the old secret are revoked, while Update
/// Client leaves the secret alone. Requires PostgreSQL + Redis; no-ops if they aren't reachable.
/// </summary>
public sealed class RotateClientSecretEndToEndTests : IAsyncLifetime
{
    private const string OriginalSecret = "original-secret";
    private const string NewSecret = "new-secret";

    private readonly GmWebApplicationFactory<Program> _factory = new();
    private Guid _clientId;
    private bool _infraReady;

    public async Task InitializeAsync()
    {
        try
        {
            _clientId = await Send(new CreateClientCommand { Name = $"secret-rotate-{Guid.NewGuid():N}", Secret = OriginalSecret });
            _infraReady = true;
        }
        catch
        {
            _infraReady = false; // Postgres/Redis unavailable — tests will no-op.
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
                await context.Set<ClientSession>().IgnoreQueryFilters().Where(s => s.ClientId == _clientId).ExecuteDeleteAsync();
                await context.Set<Client>().IgnoreQueryFilters().Where(c => c.Id == _clientId).ExecuteDeleteAsync();
            }
            catch { /* best-effort cleanup */ }
        }
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Rotate_replaces_the_old_secret_with_the_supplied_one_and_revokes_its_sessions()
    {
        if (!_infraReady) return;
        Assert.Equal(HttpStatusCode.OK, (await ClientTokenAsync(OriginalSecret)).StatusCode); // opens a session

        await Send(new RotateClientSecretCommand { ClientId = _clientId, Secret = NewSecret });

        Assert.NotEqual(HttpStatusCode.OK, (await ClientTokenAsync(OriginalSecret)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await ClientTokenAsync(NewSecret)).StatusCode);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var sessions = await context.Set<ClientSession>().IgnoreQueryFilters()
            .Where(s => s.ClientId == _clientId).OrderBy(s => s.CreatedAt).ToListAsync();
        Assert.True(sessions.First().IsRevoked, "the session issued under the old secret should be revoked");
        Assert.False(sessions.Last().IsRevoked, "the session issued under the new secret stays live");
    }

    [Fact]
    public async Task Update_client_keeps_the_current_secret()
    {
        if (!_infraReady) return;

        await Send(new UpdateClientCommand { Id = _clientId, Name = "renamed-only" });

        Assert.Equal(HttpStatusCode.OK, (await ClientTokenAsync(OriginalSecret)).StatusCode);
    }

    private Task<HttpResponseMessage> ClientTokenAsync(string secret) =>
        _factory.CreateClient().PostAsync("/connect/token", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "ClientCredentials",
            ["client_id"] = _clientId.ToString(),
            ["client_secret"] = secret,
        }));

    private async Task<TResult> Send<TResult>(IRequest<TResult> request)
    {
        using var scope = _factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IMediator>().Send(request);
    }

    private async Task Send(IRequest request)
    {
        using var scope = _factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IMediator>().Send(request);
    }
}
