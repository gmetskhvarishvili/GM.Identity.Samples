using GM.Exceptions;
using GM.Identity.Sample.Application.ConsentDocuments.Commands.CreateConsentDocument;
using GM.Identity.Sample.Application.Users.Commands.CreateUser;
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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ValidationException = GM.Exceptions.ValidationException;

namespace GM.Identity.Sample.Tests;

/// <summary>
/// End-to-end proof that consent can be recorded as part of user creation: passing a consent at its current version
/// to <see cref="CreateUserCommand"/> persists a <see cref="UserConsent"/> row (so the mandatory document is not
/// pending for the new user), while a stale version is rejected. Requires Postgres + Redis; no-ops if unavailable.
/// </summary>
public sealed class CreateUserConsentEndToEndTests : IAsyncLifetime
{
    private const string Password = "Correct123!";

    private readonly GmWebApplicationFactory<Program> _factory = new();
    private readonly string _userName = $"create-consent-{Guid.NewGuid():N}";
    private readonly string _consentType = $"ToS-{Guid.NewGuid():N}";
    private Guid _userId;
    private bool _infraReady;

    public async Task InitializeAsync()
    {
        try
        {
            using var scope = _factory.Services.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            await mediator.Send(new CreateConsentDocumentCommand
            {
                ConsentType = _consentType,
                Title = "Terms of Service",
                Content = "body",
                Version = "v1",
                IsMandatory = true,
            });
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
    public async Task Creating_a_user_with_consent_records_it_and_clears_pending()
    {
        if (!_infraReady) return;

        _userId = await SendCreateAsync(new CreateUserCommand
        {
            Username = _userName,
            Email = $"{_userName}@test.local",
            Password = Password,
            Consents = new[] { new CreateUserConsentInput { ConsentType = _consentType, DocumentVersion = "v1" } },
        });

        // The consent row was persisted.
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var recorded = await context.Set<UserConsent>().IgnoreQueryFilters()
                .SingleAsync(x => x.UserId == _userId && x.ConsentType == _consentType);
            Assert.Equal("v1", recorded.DocumentVersion);
        }

        // ...so the mandatory document is not outstanding for the new user.
        Assert.DoesNotContain(await PendingAsync(), p => p.ConsentType == _consentType);
    }

    [Fact]
    public async Task Creating_a_user_with_a_stale_consent_version_is_rejected()
    {
        if (!_infraReady) return;

        await Assert.ThrowsAsync<ValidationException>(() => SendCreateAsync(new CreateUserCommand
        {
            Username = _userName,
            Email = $"{_userName}@test.local",
            Password = Password,
            Consents = new[] { new CreateUserConsentInput { ConsentType = _consentType, DocumentVersion = "v0" } },
        }));
    }

    private async Task<Guid> SendCreateAsync(CreateUserCommand command)
    {
        using var scope = _factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        return await mediator.Send(command);
    }

    private async Task<IReadOnlyList<PendingConsentDto>> PendingAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        return await mediator.Send(new GetPendingConsentsQuery { UserId = _userId });
    }
}
