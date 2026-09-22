using GM.Identity.Authorization;
using GM.Identity.Sample.Application.ConsentDocuments.Commands.CreateConsentDocument;
using GM.Identity.Sample.Application.ConsentDocuments.Commands.UpdateConsentDocument;
using GM.Identity.Sample.Application.Users.Commands.RecordUserConsent;
using GM.Identity.Sample.Application.Users.Queries.GetPendingConsents;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.ConsentDocumentAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserConsentAggregate;
using GM.Identity.Sample.Persistence.Context;
using GM.Mediator.Contracts;
using GM.Testing;
using GM.Testing.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Tests;

/// <summary>
/// End-to-end proof of the consent gate and its admin CRUD: with Auth:EnforceConsent = true, a mandatory consent
/// document whose current version the user has not accepted refuses login and is surfaced as pending; accepting it
/// clears the gate; and bumping the document version re-opens it. Requires Postgres + Redis; no-ops if unavailable.
/// </summary>
public sealed class ConsentGateEndToEndTests : IAsyncLifetime
{
    private const string ClientId = "11111111-1111-1111-1111-111111111111";
    private const string ClientSecret = "secret";
    private const string Password = "Correct123!";

    private readonly GmWebApplicationFactory<Program> _factory =
        new GmWebApplicationFactory<Program>().WithConfig("Auth:EnforceConsent", "true");

    private readonly string _userName = $"consent-{Guid.NewGuid():N}";
    private readonly string _consentType = $"ToS-{Guid.NewGuid():N}";
    private Guid _userId;
    private Guid _documentId;
    private bool _infraReady;

    public async Task InitializeAsync()
    {
        try
        {
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var user = User.Create(_userName, $"{_userName}@test.local", null);
                var (hash, salt) = PasswordHasher.Hash(Password);
                user.UpdatePassword(hash, salt);
                await context.Set<User>().AddAsync(user);
                await context.SaveChangesAsync();
                _userId = user.Id;
            }

            using (var scope = _factory.Services.CreateScope())
            {
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                _documentId = await mediator.Send(new CreateConsentDocumentCommand
                {
                    ConsentType = _consentType,
                    Title = "Terms of Service",
                    Content = "body",
                    CurrentVersion = "v1",
                    IsMandatory = true,
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
                await context.Set<UserConsent>().IgnoreQueryFilters().Where(x => x.UserId == _userId).ExecuteDeleteAsync();
                await context.Set<ConsentDocument>().IgnoreQueryFilters()
                    .Where(x => x.ConsentType == _consentType).ExecuteDeleteAsync();
                await context.Set<User>().IgnoreQueryFilters().Where(u => u.Id == _userId).ExecuteDeleteAsync();
            }
            catch { /* best-effort cleanup */ }
        }
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Mandatory_consent_blocks_login_until_accepted_and_reopens_on_new_version()
    {
        if (!_infraReady) return;

        // Login is refused while the mandatory v1 document is unaccepted.
        Assert.False((await PasswordGrantAsync()).IsSuccessStatusCode);

        // The outstanding document is surfaced as pending.
        using (var scope = _factory.Services.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var pending = await mediator.Send(new GetPendingConsentsQuery { UserId = _userId });
            var item = Assert.Single(pending);
            Assert.Equal(_consentType, item.ConsentType);
            Assert.Equal("v1", item.CurrentVersion);
        }

        // Accepting the current version clears the gate.
        await AcceptAsync("v1");
        Assert.Empty(await GetPendingAsync());
        await (await PasswordGrantAsync()).ShouldBeOkAsync();

        // Bumping the version makes the prior acceptance outstanding again → login blocked once more.
        using (var scope = _factory.Services.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            await mediator.Send(new UpdateConsentDocumentCommand
            {
                Id = _documentId,
                Title = "Terms of Service",
                Content = "body v2",
                CurrentVersion = "v2",
                IsMandatory = true,
            });
        }

        Assert.False((await PasswordGrantAsync()).IsSuccessStatusCode);

        // Accepting the new version clears it again.
        await AcceptAsync("v2");
        await (await PasswordGrantAsync()).ShouldBeOkAsync();
    }

    private async Task AcceptAsync(string version)
    {
        using var scope = _factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        await mediator.Send(new RecordUserConsentCommand
        {
            UserId = _userId,
            ConsentType = _consentType,
            DocumentVersion = version,
        });
    }

    private async Task<IReadOnlyList<PendingConsentDto>> GetPendingAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        return await mediator.Send(new GetPendingConsentsQuery { UserId = _userId });
    }

    private Task<HttpResponseMessage> PasswordGrantAsync()
    {
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "password"),
            new KeyValuePair<string, string>("username", _userName),
            new KeyValuePair<string, string>("password", Password),
            new KeyValuePair<string, string>("client_id", ClientId),
            new KeyValuePair<string, string>("client_secret", ClientSecret),
        });
        return _factory.CreateClient().PostAsync("/connect/token", content);
    }
}
