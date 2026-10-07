using GM.Identity.Sample.Persistence.Context;
using GM.Testing;
using GM.Testing.AspNetCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Tests;

/// <summary>
/// End-to-end test for the OpenID Connect discovery document (<c>/.well-known/openid-configuration</c>): it is
/// anonymous and advertises this provider's endpoints and supported grants. Requires Postgres + Redis; no-ops
/// if they aren't reachable.
/// </summary>
public sealed class OpenIdDiscoveryEndToEndTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly GmWebApplicationFactory<Program> _factory = new();
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

    public async Task DisposeAsync() => await _factory.DisposeAsync();

    [Fact]
    public async Task Discovery_document_advertises_the_endpoints_and_grants()
    {
        if (!_infraReady) return;

        var response = await _factory.CreateClient().GetAsync("/.well-known/openid-configuration");
        await response.ShouldBeOkAsync();

        var doc = JsonSerializer.Deserialize<Discovery>(
            await response.Content.ReadAsStringAsync(), JsonOptions);

        Assert.NotNull(doc);
        Assert.False(string.IsNullOrWhiteSpace(doc!.Issuer));
        Assert.EndsWith("/connect/token", doc.TokenEndpoint);
        Assert.EndsWith("/connect/authorize", doc.AuthorizationEndpoint);
        Assert.EndsWith("/connect/introspect", doc.IntrospectionEndpoint);
        Assert.EndsWith("/connect/userinfo", doc.UserInfoEndpoint);
        Assert.Contains("authorization_code", doc.GrantTypesSupported);
        Assert.Contains("refresh_token", doc.GrantTypesSupported);
        Assert.Contains("S256", doc.CodeChallengeMethodsSupported);
    }

    private sealed record Discovery(
        [property: JsonPropertyName("issuer")] string Issuer,
        [property: JsonPropertyName("authorization_endpoint")] string AuthorizationEndpoint,
        [property: JsonPropertyName("token_endpoint")] string TokenEndpoint,
        [property: JsonPropertyName("introspection_endpoint")] string IntrospectionEndpoint,
        [property: JsonPropertyName("userinfo_endpoint")] string UserInfoEndpoint,
        [property: JsonPropertyName("grant_types_supported")] List<string> GrantTypesSupported,
        [property: JsonPropertyName("code_challenge_methods_supported")] List<string> CodeChallengeMethodsSupported);
}
