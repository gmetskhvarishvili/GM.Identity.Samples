using System;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using GM.Messaging.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GM.Identity.Sample.API.Admin;

/// <summary>
/// GM.Messaging's admin endpoints (<c>MapGMMessagingAdmin</c>) for this API's outbox: backlog, dead letters,
/// requeue and purge. They require the <see cref="Policy"/> policy, which here accepts an operations API key in the
/// <see cref="Header"/> header (<c>Admin:ApiKey</c>): user sign-in is handled by the gateways, and these endpoints are
/// for operators. A real deployment would point the policy at its own operator authentication.
/// </summary>
public static class MessagingAdmin
{
    public const string Policy = "messaging-admin";
    public const string Scheme = "AdminKey";
    public const string Header = "X-Admin-Key";

    /// <summary>The API-key scheme and the <see cref="Policy"/> policy.</summary>
    public static IServiceCollection AddMessagingAdmin(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AdminKeyOptions>(o => o.ApiKey = configuration["Admin:ApiKey"]);
        services.AddAuthentication().AddScheme<AdminKeyOptions, AdminKeyHandler>(Scheme, _ => { });
        services.AddAuthorizationBuilder()
            .AddPolicy(Policy, policy => policy.AddAuthenticationSchemes(Scheme).RequireAuthenticatedUser());
        return services;
    }

    /// <summary>Maps the outbox endpoints under <c>/admin/messaging/outbox</c>.</summary>
    public static RouteGroupBuilder MapMessagingAdmin(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGMMessagingAdmin(admin => admin.AuthorizationPolicy = Policy);
}

public sealed class AdminKeyOptions : AuthenticationSchemeOptions
{
    /// <summary>The key callers send; with none configured, nobody gets in.</summary>
    public string? ApiKey { get; set; }
}

/// <summary>Authenticates a request whose <see cref="MessagingAdmin.Header"/> matches <c>Admin:ApiKey</c>.</summary>
public sealed class AdminKeyHandler(IOptionsMonitor<AdminKeyOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AdminKeyOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (string.IsNullOrEmpty(Options.ApiKey) || !Request.Headers.TryGetValue(MessagingAdmin.Header, out var sent))
            return Task.FromResult(AuthenticateResult.NoResult());

        var expected = Encoding.UTF8.GetBytes(Options.ApiKey);
        var actual = Encoding.UTF8.GetBytes(sent.ToString());
        if (!CryptographicOperations.FixedTimeEquals(expected, actual))
            return Task.FromResult(AuthenticateResult.Fail("Wrong admin key."));

        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, "messaging-admin")], Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
    }
}
