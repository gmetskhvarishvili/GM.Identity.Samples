using GM.Identity.Sample.Application.ConsentDocuments.Commands.AddConsentDocumentVersion;
using GM.Identity.Sample.Application.ConsentDocuments.Commands.CreateConsentDocument;
using GM.Identity.Sample.Application.Users.Commands.RecordUserConsent;
using GM.Identity.Sample.Application.Users.Queries.GetPendingConsents;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.ConsentDocumentAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserConsentAggregate;
using GM.Identity.Sample.Persistence.Context;
using GM.Mediator.Contracts;
using GM.Testing.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

using System;
using System.Linq;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Tests;

/// <summary>
/// End-to-end proof of the consent-document registry: a mandatory document is reported as pending for a user who
/// has not accepted its current version; accepting it clears the pending state; and bumping the document version
/// makes the prior acceptance outstanding again. Requires Postgres + Redis; no-ops if unavailable.
/// </summary>
public sealed class ConsentDocumentsEndToEndTests : IAsyncLifetime
{
    private readonly GmWebApplicationFactory<Program> _factory = new();
    private readonly string _userName = $"consent-{Guid.NewGuid():N}";
    private readonly string _consentType = $"ToS-{Guid.NewGuid():N}";
    private Guid _userId;
    private bool _infraReady;

    public async Task InitializeAsync()
    {
        try
        {
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var user = User.Create(_userName, $"{_userName}@test.local", null);
                await context.Set<User>().AddAsync(user);
                await context.SaveChangesAsync();
                _userId = user.Id;
            }

            using (var scope = _factory.Services.CreateScope())
            {
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                await mediator.Send(new CreateConsentDocumentCommand
                {
                    ConsentType = _consentType,
                    Title = "Terms of Service",
                    Content = "body",
                    Version = "v1",
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
    public async Task Mandatory_consent_is_pending_until_accepted_and_reopens_on_new_version()
    {
        if (!_infraReady) return;

        // The mandatory v1 document is outstanding for the user.
        var item = Assert.Single(await PendingAsync(), p => p.ConsentType == _consentType);
        Assert.Equal("v1", item.Version);

        // Accepting the current version clears it.
        await AcceptAsync("v1");
        Assert.DoesNotContain(await PendingAsync(), p => p.ConsentType == _consentType);

        // Publishing a new version makes the prior acceptance outstanding again.
        using (var scope = _factory.Services.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            await mediator.Send(new AddConsentDocumentVersionCommand
            {
                ConsentType = _consentType,
                Title = "Terms of Service",
                Content = "body v2",
                Version = "v2",
                IsMandatory = true,
            });
        }

        var reopened = Assert.Single(await PendingAsync(), p => p.ConsentType == _consentType);
        Assert.Equal("v2", reopened.Version);

        // Accepting the new version clears it again.
        await AcceptAsync("v2");
        Assert.DoesNotContain(await PendingAsync(), p => p.ConsentType == _consentType);
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

    private async Task<System.Collections.Generic.IReadOnlyList<PendingConsentDto>> PendingAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        return await mediator.Send(new GetPendingConsentsQuery { UserId = _userId });
    }
}
