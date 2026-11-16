using GM.Identity.Sample.Persistence.Context;
using GM.Testing.AspNetCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Tests;

/// <summary>
/// End-to-end tests for GM.Messaging's outbox admin endpoints (<c>MapGMMessagingAdmin</c>) on the API: refused
/// without the operations key, served with it. Requires PostgreSQL; no-ops if it isn't reachable.
/// </summary>
public sealed class MessagingAdminEndToEndTests : IAsyncLifetime
{
    // Admin:ApiKey in appsettings.json.
    private const string AdminKey = "local-dev-admin-key";

    private readonly GmWebApplicationFactory<Program> _factory = new();
    private bool _infraReady;

    public async Task InitializeAsync()
    {
        try
        {
            using var scope = _factory.Services.CreateScope();
            _infraReady = await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.CanConnectAsync();
        }
        catch
        {
            _infraReady = false; // Postgres unavailable — tests will no-op.
        }
    }

    public async Task DisposeAsync() => await _factory.DisposeAsync();

    [Fact]
    public async Task OutboxBacklog_NeedsTheAdminKey()
    {
        if (!_infraReady) return;
        using var client = _factory.CreateClient();
        var url = new Uri("/admin/messaging/outbox", UriKind.Relative);

        using var anonymous = await client.GetAsync(url);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);

        using var wrongKey = new HttpRequestMessage(HttpMethod.Get, url);
        wrongKey.Headers.Add("X-Admin-Key", "wrong");
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.SendAsync(wrongKey)).StatusCode);

        using var withKey = new HttpRequestMessage(HttpMethod.Get, url);
        withKey.Headers.Add("X-Admin-Key", AdminKey);
        using var response = await client.SendAsync(withKey);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"pending\"", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }
}
