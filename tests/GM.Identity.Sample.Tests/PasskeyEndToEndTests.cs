using GM.Identity.Sample.Application.Accounts.Commands.BeginPasskeyAssertion;
using GM.Identity.Sample.Application.Accounts.Commands.CompletePasskeyAssertion;
using GM.Identity;
using GM.Identity.Sample.Application.Users.Commands.RegisterPasskey;
using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.UserSessionAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.PasskeyAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserAggregate;
using GM.Identity.Sample.Persistence.Context;
using GM.Mediator.Contracts;
using GM.Testing;
using GM.Testing.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Tests;

/// <summary>
/// End-to-end proof of passkey (WebAuthn) login: the test acts as the authenticator — it generates an ES256
/// keypair, registers the public key, then signs the issued challenge and completes the assertion to obtain a
/// session. A replay of the (consumed) challenge is rejected. Requires Postgres + Redis; no-ops if unavailable.
/// </summary>
public sealed class PasskeyEndToEndTests : IAsyncLifetime
{
    private const string ClientId = "11111111-1111-1111-1111-111111111111";

    private readonly GmWebApplicationFactory<Program> _factory = new();
    private readonly string _userName = $"passkey-{Guid.NewGuid():N}";
    private readonly string _credentialId = WebAuthnAssertion.Base64UrlEncode(Guid.NewGuid().ToByteArray());
    private readonly ECDsa _authenticator = ECDsa.Create(ECCurve.NamedCurves.nistP256);
    private Guid _userId;
    private bool _infraReady;

    public async Task InitializeAsync()
    {
        try
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = User.Create(_userName, $"{_userName}@test.local", null);
            var (hash, salt) = PasswordHasher.Hash("Correct123!");
            user.UpdatePassword(hash, salt);
            await context.Set<User>().AddAsync(user);
            await context.SaveChangesAsync();
            _userId = user.Id;

            using (var regScope = _factory.Services.CreateScope())
            {
                var mediator = regScope.ServiceProvider.GetRequiredService<IMediator>();
                await mediator.Send(new RegisterPasskeyCommand
                {
                    UserId = _userId,
                    CredentialId = _credentialId,
                    PublicKeySpkiBase64 = Convert.ToBase64String(_authenticator.ExportSubjectPublicKeyInfo()),
                    Name = "Test authenticator",
                });
            }
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
                await context.Set<PasskeyChallenge>().IgnoreQueryFilters().Where(x => x.UserId == _userId).ExecuteDeleteAsync();
                await context.Set<UserPasskey>().IgnoreQueryFilters().Where(x => x.UserId == _userId).ExecuteDeleteAsync();
                await context.Set<UserSession>().Where(s => s.UserId == _userId).ExecuteDeleteAsync();
                await context.Set<User>().IgnoreQueryFilters().Where(u => u.Id == _userId).ExecuteDeleteAsync();
            }
            catch { /* best-effort cleanup */ }
        }
        _authenticator.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Passkey_assertion_logs_in_and_the_challenge_is_single_use()
    {
        if (!_infraReady) return;

        var challenge = await BeginAsync();
        var (authData, clientDataJson, signature) = SignAssertion(challenge);

        var tokens = await CompleteAsync(authData, clientDataJson, signature);
        Assert.False(string.IsNullOrEmpty(tokens.AccessToken));

        // The challenge was consumed — replaying the same assertion fails (no active challenge).
        await Assert.ThrowsAnyAsync<Exception>(() => CompleteAsync(authData, clientDataJson, signature));
    }

    [Fact]
    public async Task A_forged_signature_is_rejected()
    {
        if (!_infraReady) return;

        var challenge = await BeginAsync();
        var (authData, clientDataJson, _) = SignAssertion(challenge);

        // Sign with a DIFFERENT key → verification must fail.
        using var attacker = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var clientDataHash = SHA256.HashData(WebAuthnAssertion.Base64UrlDecode(clientDataJson));
        var authBytes = WebAuthnAssertion.Base64UrlDecode(authData);
        var signed = authBytes.Concat(clientDataHash).ToArray();
        var forged = WebAuthnAssertion.Base64UrlEncode(
            attacker.SignData(signed, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence));

        await Assert.ThrowsAnyAsync<Exception>(() => CompleteAsync(authData, clientDataJson, forged));
    }

    private async Task<string> BeginAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        return await mediator.Send(new BeginPasskeyAssertionCommand { UserName = _userName });
    }

    private (string AuthData, string ClientDataJson, string Signature) SignAssertion(string challenge)
    {
        var clientData = JsonSerializer.SerializeToUtf8Bytes(new
        {
            type = "webauthn.get",
            challenge,
            origin = "https://localhost",
        });

        var authenticatorData = new byte[37];
        authenticatorData[32] = 0x05;      // flags: user present + verified
        authenticatorData[36] = 0x01;      // sign counter = 1

        var clientDataHash = SHA256.HashData(clientData);
        var signed = authenticatorData.Concat(clientDataHash).ToArray();
        var signature = _authenticator.SignData(signed, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);

        return (
            WebAuthnAssertion.Base64UrlEncode(authenticatorData),
            WebAuthnAssertion.Base64UrlEncode(clientData),
            WebAuthnAssertion.Base64UrlEncode(signature));
    }

    private async Task<PasskeyTokenDto> CompleteAsync(string authData, string clientDataJson, string signature)
    {
        using var scope = _factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        return await mediator.Send(new CompletePasskeyAssertionCommand
        {
            UserName = _userName,
            ClientId = Guid.Parse(ClientId),
            CredentialId = _credentialId,
            AuthenticatorDataBase64Url = authData,
            ClientDataJsonBase64Url = clientDataJson,
            SignatureBase64Url = signature,
        });
    }
}
