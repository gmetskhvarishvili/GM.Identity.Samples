using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.ClientAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.ClientRedirectUriAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserAggregate;
using GM.Identity.Sample.Persistence.Context;
using GM.Testing;
using GM.Testing.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Tests;

/// <summary>
/// Create and Update Client carry the same client settings as dynamic registration — redirect URIs, back- and
/// front-channel logout URIs and the consent flag — over HTTP (the API models used to drop them). Calls run as the
/// seeded admin through the seeded client, which hold every permission and scope. Requires PostgreSQL + Redis;
/// no-ops if they aren't reachable.
/// </summary>
public sealed class ClientSettingsEndToEndTests : IAsyncLifetime
{
    private const string SeededClientId = "11111111-1111-1111-1111-111111111111";
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);
    private static readonly string[] CreateUris = ["https://app.example/callback", "https://app.example/callback", " "];
    private static readonly string[] CallbackOnly = ["https://app.example/callback"];
    private static readonly string[] KeepAndDrop = ["https://app.example/keep", "https://app.example/drop"];
    private static readonly string[] KeepAndNew = ["https://app.example/keep", "https://app.example/new"];
    private static readonly string[] RelativeOnly = ["/callback"];

    private readonly GmWebApplicationFactory<Program> _factory = new();
    private readonly string _name = $"settings-{Guid.NewGuid():N}";
    private Guid _adminId;
    private Guid _clientId;
    private bool _infraReady;

    public async Task InitializeAsync()
    {
        try
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            _adminId = await context.Set<User>().IgnoreQueryFilters()
                .Where(u => u.Email == "admin@local").Select(u => u.Id).FirstAsync();
            _infraReady = true;
        }
        catch
        {
            _infraReady = false; // Postgres/Redis unavailable — tests will no-op.
        }
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
                await context.Set<Client>().IgnoreQueryFilters().Where(x => x.Id == _clientId).ExecuteDeleteAsync();
            }
            catch { /* best-effort cleanup */ }
        }
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Create_stores_redirect_and_logout_uris_and_consent()
    {
        if (!_infraReady) return;

        await CreateAsync(new
        {
            name = _name,
            secret = "settings-secret",
            redirectUris = CreateUris,
            backchannelLogoutUri = "https://app.example/logout/back",
            frontchannelLogoutUri = "https://app.example/logout/front",
            requireConsent = true,
        });

        var details = await DetailsAsync();
        Assert.Equal(CallbackOnly, details.RedirectUris); // trimmed + de-duplicated
        Assert.Equal("https://app.example/logout/back", details.BackchannelLogoutUri);
        Assert.Equal("https://app.example/logout/front", details.FrontchannelLogoutUri);
        Assert.True(details.RequireConsent);
    }

    [Fact]
    public async Task Update_replaces_redirect_uris_and_changes_settings()
    {
        if (!_infraReady) return;
        await CreateAsync(new
        {
            name = _name,
            secret = "settings-secret",
            redirectUris = KeepAndDrop,
            backchannelLogoutUri = "https://app.example/logout/back",
        });

        await SendAsync(HttpMethod.Put, $"/api/v1/Clients/{_clientId}", new
        {
            name = _name,
            redirectUris = KeepAndNew,
            backchannelLogoutUri = "",                                   // empty clears it
            frontchannelLogoutUri = "https://app.example/logout/front",
            requireConsent = true,
        });

        var details = await DetailsAsync();
        Assert.Equal(KeepAndNew, details.RedirectUris);
        Assert.Null(details.BackchannelLogoutUri);
        Assert.Equal("https://app.example/logout/front", details.FrontchannelLogoutUri);
        Assert.True(details.RequireConsent);
    }

    [Fact]
    public async Task Update_without_the_fields_leaves_them_unchanged()
    {
        if (!_infraReady) return;
        await CreateAsync(new
        {
            name = _name,
            secret = "settings-secret",
            redirectUris = CallbackOnly,
            frontchannelLogoutUri = "https://app.example/logout/front",
            requireConsent = true,
        });

        await SendAsync(HttpMethod.Put, $"/api/v1/Clients/{_clientId}", new { name = $"{_name}-renamed" });

        var details = await DetailsAsync();
        Assert.Equal(CallbackOnly, details.RedirectUris);
        Assert.Equal("https://app.example/logout/front", details.FrontchannelLogoutUri);
        Assert.True(details.RequireConsent);
    }

    [Fact]
    public async Task A_relative_redirect_uri_is_rejected()
    {
        if (!_infraReady) return;

        var response = await SendAsync(HttpMethod.Post, "/api/v1/Clients",
            new { name = _name, secret = "settings-secret", redirectUris = RelativeOnly }, ensureOk: false);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task CreateAsync(object body)
    {
        var response = await SendAsync(HttpMethod.Post, "/api/v1/Clients", body);
        _clientId = Guid.Parse((await response.Content.ReadAsStringAsync()).Trim('"'));
    }

    private async Task<Details> DetailsAsync()
    {
        var response = await SendAsync(HttpMethod.Get, $"/api/v1/Clients/{_clientId}", body: null);
        return JsonSerializer.Deserialize<Details>(await response.Content.ReadAsStringAsync(), Web)!;
    }

    // Plays the gateway: the seeded admin (every permission) acting through the seeded client (every scope).
    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, object? body, bool ensureOk = true)
    {
        using var request = new HttpRequestMessage(method, url);
        request.Headers.Add("X-User-Id", _adminId.ToString("D"));
        request.Headers.Add("X-Client-Id", SeededClientId);
        if (body is not null)
            request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        var response = await _factory.CreateClient().SendAsync(request);
        if (ensureOk) await response.ShouldBeOkAsync();
        return response;
    }

    private sealed record Details(
        Guid Id, List<string> RedirectUris, string? BackchannelLogoutUri, string? FrontchannelLogoutUri, bool RequireConsent);
}
