using GM.Exceptions;
using GM.Identity.Sample.Application.Infrastructure.Services.OAuth;
using GM.Testing.AspNetCore;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Xunit;

using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Tests;

/// <summary>
/// Proves the newly added external identity providers (Microsoft, Apple, LinkedIn, GitHub, and a generic
/// enterprise OIDC provider) are wired purely from configuration: each produces a valid PKCE authorization URL
/// pointing at its own endpoint, and provider-specific extras (Google's access_type, Apple's response_mode) are
/// applied. An unknown provider is rejected. Requires Redis (the verifier is stored there); no-ops otherwise.
/// </summary>
public sealed class ExternalProviderRedirectEndToEndTests : IAsyncLifetime
{
    private const string RedirectUri = "https://client.example/external/callback";

    private readonly GmWebApplicationFactory<Program> _factory = new();
    private bool _infraReady;

    public Task InitializeAsync()
    {
        try
        {
            _infraReady = _factory.Services.GetRequiredService<IConnectionMultiplexer>().IsConnected;
        }
        catch
        {
            _infraReady = false;
        }
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _factory.DisposeAsync();

    [Theory]
    [InlineData("Microsoft", "https://login.microsoftonline.com/common/oauth2/v2.0/authorize")]
    [InlineData("LinkedIn", "https://www.linkedin.com/oauth/v2/authorization")]
    [InlineData("Apple", "https://appleid.apple.com/auth/authorize")]
    [InlineData("GitHub", "https://github.com/login/oauth/authorize")]
    [InlineData("EnterpriseSso", "https://YOUR_IDP/authorize")]
    public async Task Each_provider_builds_a_pkce_authorization_url(string provider, string expectedEndpoint)
    {
        if (!_infraReady) return;

        var url = await GetRedirectUriAsync(provider);

        Assert.StartsWith(expectedEndpoint, url);
        Assert.Contains("client_id=", url);
        Assert.Contains("response_type=code", url);
        Assert.Contains("code_challenge=", url);
        Assert.Contains("code_challenge_method=S256", url);
        Assert.Contains("state=", url);
    }

    [Fact]
    public async Task Provider_specific_authorization_parameters_are_applied()
    {
        if (!_infraReady) return;

        // Apple needs form_post; Google needs offline access + consent — both come from config, not code.
        Assert.Contains("response_mode=form_post", await GetRedirectUriAsync("Apple"));

        var google = await GetRedirectUriAsync("Google");
        Assert.Contains("access_type=offline", google);
        Assert.Contains("prompt=consent", google);
    }

    [Fact]
    public async Task An_unknown_provider_is_rejected()
    {
        if (!_infraReady) return;

        await Assert.ThrowsAsync<CustomException>(() => GetRedirectUriAsync("NotAProvider"));
    }

    private async Task<string> GetRedirectUriAsync(string provider)
    {
        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IOAuthService>();
        return await service.GetRedirectUri(
            new GetRedirectUriDto { Provider = provider, RedirectUri = RedirectUri }, CancellationToken.None);
    }
}
