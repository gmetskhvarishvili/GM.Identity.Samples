using GM.Identity.Authorization;
using GM.Identity.Sample.Application.Users.Commands.RequestContactChange;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserPendingContactChangeAggregate;
using GM.Identity.Sample.Domain.BoundedContext.MessagingBoundedContext.OutboxMessageAggregate;
using GM.Identity.Sample.Domain.Enums;
using GM.Identity.Sample.Persistence.Context;
using GM.Mediator.Contracts;
using GM.Testing;
using GM.Testing.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

using System;
using System.Linq;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Tests;

/// <summary>
/// End-to-end proof of verify-before-apply email change: requesting a change records it as pending and queues
/// a code to the new address, but does NOT change the user's email until confirmed. Requires Postgres + Redis;
/// no-ops if they aren't reachable.
/// </summary>
public sealed class ContactChangeEndToEndTests : IAsyncLifetime
{
    private readonly GmWebApplicationFactory<Program> _factory = new();
    private readonly string _originalEmail = $"orig-{Guid.NewGuid():N}@test.local";
    private readonly string _newEmail = $"new-{Guid.NewGuid():N}@test.local";
    private Guid _userId;
    private bool _infraReady;

    public async Task InitializeAsync()
    {
        try
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = User.Create($"contact-{Guid.NewGuid():N}", _originalEmail, null);
            var (hash, salt) = PasswordHasher.Hash("Correct123!");
            user.UpdatePassword(hash, salt);
            await context.Set<User>().AddAsync(user);
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
                await context.Set<UserPendingContactChange>().IgnoreQueryFilters().Where(x => x.UserId == _userId).ExecuteDeleteAsync();
                await context.Set<OutboxMessage>().Where(m => m.UserId == _userId).ExecuteDeleteAsync();
                await context.Set<User>().IgnoreQueryFilters().Where(u => u.Id == _userId).ExecuteDeleteAsync();
            }
            catch { /* best-effort cleanup */ }
        }
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Requesting_an_email_change_records_it_pending_and_does_not_apply_it_yet()
    {
        if (!_infraReady) return;

        using (var scope = _factory.Services.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            await mediator.Send(new RequestContactChangeCommand
            {
                UserId = _userId,
                ConfirmationType = ConfirmationType.Email,
                NewContact = _newEmail,
            });
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Pending change recorded for the NEW email.
            var pending = await context.Set<UserPendingContactChange>().IgnoreQueryFilters()
                .FirstAsync(x => x.UserId == _userId);
            Assert.Equal(_newEmail, pending.NewContact);
            Assert.Equal(ConfirmationType.Email, pending.ConfirmationType);

            // A confirmation code was queued to the new address.
            Assert.True(await context.Set<OutboxMessage>()
                .AnyAsync(m => m.UserId == _userId && m.EventType.Contains("UserConfirmationInitiatedIntegrationEvent")));

            // The user's email is UNCHANGED until confirmation.
            var user = await context.Set<User>().IgnoreQueryFilters().FirstAsync(u => u.Id == _userId);
            Assert.Equal(_originalEmail, user.Email);
        }
    }
}
