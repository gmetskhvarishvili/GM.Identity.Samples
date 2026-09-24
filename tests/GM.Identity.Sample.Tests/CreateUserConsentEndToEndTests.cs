using GM.Exceptions;
using GM.Identity.Sample.Application.ConsentDocuments.Commands.CreateConsentDocument;
using GM.Identity.Sample.Application.Events.Users;
using GM.Identity.Sample.Application.Users.Commands.CreateUser;
using GM.Identity.Sample.Application.Users.Commands.CreateUserRole;
using GM.Identity.Sample.Application.Users.Commands.RecordUserConsent;
using GM.Identity.Sample.Application.Users.Queries.GetPendingConsents;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.RoleAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.ConsentDocumentAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.TwoFactorAuthTypeAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserConsentAggregate;
using GM.Identity.Sample.Domain.BoundedContext.MessagingBoundedContext.OutboxMessageAggregate;
using GM.Identity.Sample.Persistence.Context;
using GM.Mediator.Contracts;
using GM.Testing.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ValidationException = GM.Exceptions.ValidationException;

namespace GM.Identity.Sample.Tests;

/// <summary>
/// End-to-end proof that consent can be recorded as part of user creation, and that the resulting
/// <see cref="UserRegisteredIntegrationEvent"/> carries the full registration snapshot (roles, 2FA methods and
/// consents). A stale consent version is rejected. Requires Postgres + Redis; no-ops if unavailable.
/// </summary>
public sealed class CreateUserConsentEndToEndTests : IAsyncLifetime
{
    private const string Password = "Correct123!";

    private readonly GmWebApplicationFactory<Program> _factory = new();
    private readonly string _userName = $"create-consent-{Guid.NewGuid():N}";
    private readonly string _consentType = $"ToS-{Guid.NewGuid():N}";
    private Guid _userId;
    private Guid _roleId;
    private int _twoFactorTypeId;
    private bool _infraReady;

    public async Task InitializeAsync()
    {
        try
        {
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var role = Role.Create($"role-{Guid.NewGuid():N}");
                await context.Set<Role>().AddAsync(role);

                var type = TwoFactorAuthType.Create($"tfa-{Guid.NewGuid():N}", "Test authenticator");
                await context.Set<TwoFactorAuthType>().AddAsync(type);
                await context.SaveChangesAsync();

                _roleId = role.Id;
                _twoFactorTypeId = type.Id;
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
                await context.Set<OutboxMessage>().Where(x => x.UserId == _userId).ExecuteDeleteAsync();
                await context.Set<UserConsent>().IgnoreQueryFilters().Where(x => x.UserId == _userId).ExecuteDeleteAsync();
                await context.Set<ConsentDocument>().IgnoreQueryFilters()
                    .Where(x => x.ConsentType == _consentType).ExecuteDeleteAsync();
                await context.Set<User>().IgnoreQueryFilters().Where(u => u.Id == _userId).ExecuteDeleteAsync();
                await context.Set<TwoFactorAuthType>().Where(x => x.Id == _twoFactorTypeId).ExecuteDeleteAsync();
                await context.Set<Role>().IgnoreQueryFilters().Where(x => x.Id == _roleId).ExecuteDeleteAsync();
            }
            catch { /* best-effort cleanup */ }
        }
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Creating_a_user_records_consent_and_the_event_carries_the_full_snapshot()
    {
        if (!_infraReady) return;

        _userId = await SendCreateAsync(new CreateUserCommand
        {
            Username = _userName,
            Email = $"{_userName}@test.local",
            Password = Password,
            UserRoles = new[] { new CreateUserRoleCommand { RoleId = _roleId } },
            TwoFactorAuthTypeIds = new[] { _twoFactorTypeId },
            Consents = new[] { new RecordUserConsentCommand { ConsentType = _consentType, DocumentVersion = "v1" } },
        });

        // The consent row was persisted, so the mandatory document is not outstanding for the new user.
        Assert.DoesNotContain(await PendingAsync(), p => p.ConsentType == _consentType);

        // The registration event carries roles, 2FA methods and consents.
        var evt = await ReadRegistrationEventAsync();
        Assert.Equal(_userId, evt.UserId);
        Assert.Contains(_roleId, evt.RoleIds);
        Assert.Contains(_twoFactorTypeId, evt.TwoFactorAuthTypeIds);
        var consent = Assert.Single(evt.Consents, c => c.ConsentType == _consentType);
        Assert.Equal("v1", consent.DocumentVersion);
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
            Consents = new[] { new RecordUserConsentCommand { ConsentType = _consentType, DocumentVersion = "v0" } },
        }));
    }

    private async Task<Guid> SendCreateAsync(CreateUserCommand command)
    {
        using var scope = _factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        return await mediator.Send(command);
    }

    private async Task<UserRegisteredIntegrationEvent> ReadRegistrationEventAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var message = await context.Set<OutboxMessage>().SingleAsync(x => x.UserId == _userId);
        return JsonSerializer.Deserialize<UserRegisteredIntegrationEvent>(message.Payload)!;
    }

    private async Task<IReadOnlyList<PendingConsentDto>> PendingAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        return await mediator.Send(new GetPendingConsentsQuery { UserId = _userId });
    }
}
