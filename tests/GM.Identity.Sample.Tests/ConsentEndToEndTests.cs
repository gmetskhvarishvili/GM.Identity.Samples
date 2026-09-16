using GM.Exceptions;
using GM.Identity;
using GM.Identity.Sample.Application.Accounts.Commands.AuthorizeCode;
using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.AuthorizationCodeAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.SsoSessionAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.UserClientConsentAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.ClientAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.ClientRedirectUriAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserAggregate;
using GM.Identity.Sample.Persistence.Context;
using GM.Mediator.Contracts;
using GM.Testing.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

using System;
using System.Linq;
using System.Threading.Tasks;
using ValidationException = GM.Exceptions.ValidationException;

namespace GM.Identity.Sample.Tests;

/// <summary>
/// End-to-end proof of per-client OAuth consent: a client marked RequireConsent refuses to mint a code until the
/// user approves the requested scopes; once approved the grant is remembered, so a later request within those
/// scopes proceeds silently, while a request for a NEW scope needs fresh approval. Requires Postgres + Redis.
/// </summary>
public sealed class ConsentEndToEndTests : IAsyncLifetime
{
    private const string Password = "Correct123!";

    private readonly GmWebApplicationFactory<Program> _factory = new();
    private readonly string _userName = $"consent-{Guid.NewGuid():N}";
    private readonly string _redirectUri = $"https://client.example/cb/{Guid.NewGuid():N}";
    private Guid _userId;
    private Guid _clientId;
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

            var client = Client.Create($"consent-client-{Guid.NewGuid():N}");
            var (secretHash, secretSalt) = PasswordHasher.Hash("secret");
            client.UpdateSecret(secretHash, secretSalt);
            client.SetRequireConsent(true);
            await context.Set<Client>().AddAsync(client);
            await context.Set<ClientRedirectUri>().AddAsync(ClientRedirectUri.Create(client.Id, _redirectUri));
            await context.SaveChangesAsync();

            _userId = user.Id;
            _clientId = client.Id;
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
                await context.Set<UserClientConsent>().IgnoreQueryFilters().Where(c => c.UserId == _userId).ExecuteDeleteAsync();
                await context.Set<SsoSession>().IgnoreQueryFilters().Where(s => s.UserId == _userId).ExecuteDeleteAsync();
                await context.Set<ClientRedirectUri>().IgnoreQueryFilters().Where(x => x.ClientId == _clientId).ExecuteDeleteAsync();
                await context.Set<Client>().IgnoreQueryFilters().Where(c => c.Id == _clientId).ExecuteDeleteAsync();
                await context.Set<User>().IgnoreQueryFilters().Where(u => u.Id == _userId).ExecuteDeleteAsync();
            }
            catch { /* best-effort cleanup */ }
        }
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Consent_is_required_once_then_remembered_and_re_prompted_for_new_scopes()
    {
        if (!_infraReady) return;

        // Without approval, a consent-requiring client is refused.
        var refused = await Assert.ThrowsAsync<ValidationException>(() => AuthorizeAsync("openid email", consent: false));
        Assert.Contains("consent_required", refused.Message);

        // With approval, a code is minted and the grant is remembered.
        Assert.False(string.IsNullOrWhiteSpace((await AuthorizeAsync("openid email", consent: true)).Code));

        // A later request within the granted scopes proceeds silently (no approval flag needed).
        Assert.False(string.IsNullOrWhiteSpace((await AuthorizeAsync("openid email", consent: false)).Code));

        // A NEW scope beyond what was granted needs fresh approval.
        await Assert.ThrowsAsync<ValidationException>(() => AuthorizeAsync("openid email profile", consent: false));
        Assert.False(string.IsNullOrWhiteSpace((await AuthorizeAsync("openid email profile", consent: true)).Code));
    }

    private async Task<AuthorizeCodeResponseDto> AuthorizeAsync(string scope, bool consent)
    {
        using var scope1 = _factory.Services.CreateScope();
        var mediator = scope1.ServiceProvider.GetRequiredService<IMediator>();
        return await mediator.Send(new AuthorizeCodeCommand
        {
            ClientId = _clientId,
            RedirectUri = _redirectUri,
            Scope = scope,
            CodeChallenge = PkceHelper.GenerateCodeChallenge(PkceHelper.GenerateCodeVerifier()),
            UserName = _userName,
            Password = Password,
            Consent = consent,
        });
    }
}
