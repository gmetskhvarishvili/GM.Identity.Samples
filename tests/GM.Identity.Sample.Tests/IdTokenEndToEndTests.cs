using GM.Identity;
using GM.Identity.Sample.Application.Accounts.Commands.Authorize;
using GM.Identity.Sample.Application.Accounts.Commands.AuthorizeCode;
using GM.Identity.Oidc;
using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.AuthorizationCodeAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.UserSessionAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.ClientRedirectUriAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserAggregate;
using GM.Identity.Sample.Persistence.Context;
using GM.Mediator.Contracts;
using GM.Testing.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Xunit;

using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Tests;

/// <summary>
/// End-to-end proof that the token endpoint issues a signed OpenID Connect id_token alongside the opaque access
/// token: the PKCE authorization-code flow (with a nonce) yields an ES256 id_token verifiable against the OP's
/// JWKS, carrying the subject, session, echoed nonce and email. The password grant issues one too.
/// Requires Postgres + Redis; no-ops otherwise.
/// </summary>
public sealed class IdTokenEndToEndTests : IAsyncLifetime
{
    private const string ClientId = "11111111-1111-1111-1111-111111111111";
    private const string ClientSecret = "secret";
    private const string Password = "Correct123!";
    private const string Issuer = "https://op.test";

    private readonly GmWebApplicationFactory<Program> _factory = new();
    private readonly string _userName = $"idt-{Guid.NewGuid():N}";
    private readonly string _redirectUri = $"https://client.example/cb/{Guid.NewGuid():N}";
    private readonly Guid _clientId = Guid.Parse(ClientId);
    private Guid _userId;
    private bool _infraReady;

    public async Task InitializeAsync()
    {
        try
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var user = User.Create(_userName, $"{_userName}@test.local", null);
            var (hash, salt) = PasswordHasher.Hash(Password);
            user.UpdatePassword(hash, salt);
            await context.Set<User>().AddAsync(user);
            await context.Set<ClientRedirectUri>().AddAsync(ClientRedirectUri.Create(_clientId, _redirectUri));
            await context.SaveChangesAsync();
            _userId = user.Id;
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
                await context.Set<AuthorizationCode>().IgnoreQueryFilters().Where(a => a.UserId == _userId).ExecuteDeleteAsync();
                await context.Set<UserSession>().Where(s => s.UserId == _userId).ExecuteDeleteAsync();
                await context.Set<ClientRedirectUri>().IgnoreQueryFilters().Where(x => x.Uri == _redirectUri).ExecuteDeleteAsync();
                await context.Set<User>().IgnoreQueryFilters().Where(u => u.Id == _userId).ExecuteDeleteAsync();
            }
            catch { /* best-effort cleanup */ }
        }
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Authorization_code_flow_issues_a_verifiable_id_token_with_the_nonce()
    {
        if (!_infraReady) return;

        var verifier = PkceHelper.GenerateCodeVerifier();
        var nonce = Guid.NewGuid().ToString("N");

        string code;
        using (var scope = _factory.Services.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var authorize = await mediator.Send(new AuthorizeCodeCommand
            {
                ClientId = _clientId,
                RedirectUri = _redirectUri,
                CodeChallenge = PkceHelper.GenerateCodeChallenge(verifier),
                UserName = _userName,
                Password = Password,
                Nonce = nonce,
            });
            code = authorize.Code;
        }

        AuthorizeResponseDto tokens;
        using (var scope = _factory.Services.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            tokens = await mediator.Send(new AuthorizeCommand
            {
                GrantType = "authorization_code",
                Code = code,
                RedirectUri = _redirectUri,
                CodeVerifier = verifier,
                ClientId = _clientId,
                ClientSecret = ClientSecret,
                Issuer = Issuer,
            });
        }

        Assert.False(string.IsNullOrEmpty(tokens.IdToken));

        var jwt = await ValidateAsync(tokens.IdToken!);
        Assert.Equal(_userId.ToString(), jwt.Subject);
        Assert.Equal(nonce, jwt.GetClaim("nonce").Value);
        Assert.False(string.IsNullOrEmpty(jwt.GetClaim("sid").Value));
        Assert.Equal($"{_userName}@test.local", jwt.GetClaim("email").Value);
    }

    [Fact]
    public async Task Password_grant_also_issues_an_id_token()
    {
        if (!_infraReady) return;

        using var scope = _factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var tokens = await mediator.Send(new AuthorizeCommand
        {
            GrantType = "password",
            UserName = _userName,
            Password = Password,
            ClientId = _clientId,
            ClientSecret = ClientSecret,
            Issuer = Issuer,
        });

        Assert.False(string.IsNullOrEmpty(tokens.IdToken));
        var jwt = await ValidateAsync(tokens.IdToken!);
        Assert.Equal(_userId.ToString(), jwt.Subject);
    }

    // Verifies the id_token exactly as a relying party would: against the OP's published JWKS.
    private async Task<JsonWebToken> ValidateAsync(string idToken)
    {
        var jwk = _factory.Services.GetRequiredService<IJwksProvider>().GetKeys().Keys.Single();
        using var ecdsa = ECDsa.Create(new ECParameters
        {
            Curve = ECCurve.NamedCurves.nistP256,
            Q = new ECPoint { X = Base64UrlEncoder.DecodeBytes(jwk.X), Y = Base64UrlEncoder.DecodeBytes(jwk.Y) },
        });

        var result = await new JsonWebTokenHandler().ValidateTokenAsync(idToken, new TokenValidationParameters
        {
            ValidIssuer = Issuer,
            ValidAudience = ClientId,
            IssuerSigningKey = new ECDsaSecurityKey(ecdsa) { KeyId = jwk.KeyId },
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
        });

        Assert.True(result.IsValid);
        return (JsonWebToken)result.SecurityToken;
    }
}
