using GM.Exceptions;
using GM.Identity.Sample.Application.Infrastructure.Services.OAuth;
using GM.Testing.AspNetCore;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Xunit;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Tests;

/// <summary>
/// Proves the external-login (social SSO) PKCE state is held in a distributed, single-use store rather than a
/// process-local dictionary. The verifier is written under an opaque <c>state</c> at redirect time and consumed
/// atomically at callback time (GETDEL) from a <b>different</b> DI scope — so it survives across instances and
/// cannot be replayed. An unknown/expired/already-consumed state is rejected. Requires Redis; no-ops otherwise.
/// </summary>
public sealed class ExternalLoginStateEndToEndTests : IAsyncLifetime
{
    private const string Provider = "Google";
    private const string RedirectUri = "https://client.example/external/callback";

    private readonly GmWebApplicationFactory<Program> _factory = new();
    private bool _infraReady;

    public Task InitializeAsync()
    {
        try
        {
            var redis = _factory.Services.GetRequiredService<IConnectionMultiplexer>();
            _infraReady = redis.IsConnected;
        }
        catch
        {
            _infraReady = false;
        }
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _factory.DisposeAsync();

    [Fact]
    public async Task An_unknown_state_is_rejected()
    {
        if (!_infraReady) return;

        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IOAuthService>();

        var ex = await Assert.ThrowsAsync<CustomException>(() => service.GetEmail(
            new GetEmailDto { Provider = Provider, State = Guid.NewGuid().ToString("N"), Code = "x", RedirectUri = RedirectUri },
            CancellationToken.None));
        Assert.Contains("Missing or invalid state", ex.Message);
    }

    [Fact]
    public async Task State_is_shared_across_scopes_and_single_use()
    {
        if (!_infraReady) return;

        // Redirect leg (one scope/instance) issues and stores the state.
        string state;
        using (var scope = _factory.Services.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<IOAuthService>();
            var url = await service.GetRedirectUri(
                new GetRedirectUriDto { Provider = Provider, RedirectUri = RedirectUri }, CancellationToken.None);
            state = ExtractQueryValue(url, "state");
            Assert.False(string.IsNullOrWhiteSpace(state));
        }

        // Callback leg (a DIFFERENT scope) consumes the state. It gets PAST the state check — the failure now
        // comes from the downstream provider exchange, not from a missing state — proving the state crossed
        // instance boundaries via the shared store.
        using (var scope = _factory.Services.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<IOAuthService>();

            var firstUse = await Record.ExceptionAsync(() => service.GetEmail(
                new GetEmailDto { Provider = Provider, State = state, Code = "dummy-code", RedirectUri = RedirectUri },
                CancellationToken.None));
            Assert.NotNull(firstUse);
            Assert.DoesNotContain("Missing or invalid state", firstUse!.Message);

            // Single-use: the state was consumed by the first callback, so replaying it is rejected.
            var replay = await Assert.ThrowsAsync<CustomException>(() => service.GetEmail(
                new GetEmailDto { Provider = Provider, State = state, Code = "dummy-code", RedirectUri = RedirectUri },
                CancellationToken.None));
            Assert.Contains("Missing or invalid state", replay.Message);
        }
    }

    private static string ExtractQueryValue(string url, string key)
    {
        var query = new Uri(url).Query.TrimStart('?');
        return query.Split('&')
            .Select(pair => pair.Split('=', 2))
            .Where(kv => kv.Length == 2 && kv[0] == key)
            .Select(kv => Uri.UnescapeDataString(kv[1]))
            .FirstOrDefault() ?? string.Empty;
    }
}
