using GM.Exceptions;
using GM.Identity.Sample.Application.Clients.Commands.CreateClientScope;
using GM.Identity.Sample.Application.Operations.Commands.CreateOperation;
using GM.Identity.Sample.Application.Users.Commands.CreateUser;
using GM.Identity.Sample.Application.Users.Commands.CreateUserRole;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.RoleAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.ScopeAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.UserRoleAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.OperationAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.ClientScopeAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.ClientAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserAggregate;
using GM.Identity.Sample.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Xunit;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Tests;

/// <summary>
/// Infrastructure-free unit tests for the command handlers (real EF over in-memory SQLite + in-memory
/// cache fakes, see <see cref="InMemoryHandlerHost"/>). They pin the behaviours the WAF tests can't cheaply
/// isolate: duplicate guards, child-collection persistence, and Redis write-through.
/// </summary>
public sealed class HandlerUnitTests
{
    [Fact]
    public async Task CreateClientScope_persists_and_writes_through_to_the_scope_cache()
    {
        using var host = new InMemoryHandlerHost();
        var (clientId, scopeId) = await SeedClientAndScopeAsync(host);

        var handler = new CreateClientScopeCommandHandler(host.Uow, host.ScopeCache);
        await handler.Handle(new CreateClientScopeCommand { ClientId = clientId, ScopeId = scopeId }, CancellationToken.None);

        Assert.True(await host.Context.Set<ClientScope>()
            .AnyAsync(x => x.ClientId == clientId && x.ScopeId == scopeId));
        Assert.Contains(scopeId, host.ScopeCache.ClientScopes(clientId)); // write-through
    }

    [Fact]
    public async Task CreateClientScope_rejects_a_duplicate_grant()
    {
        using var host = new InMemoryHandlerHost();
        var (clientId, scopeId) = await SeedClientAndScopeAsync(host);
        var handler = new CreateClientScopeCommandHandler(host.Uow, host.ScopeCache);

        await handler.Handle(new CreateClientScopeCommand { ClientId = clientId, ScopeId = scopeId }, CancellationToken.None);

        await Assert.ThrowsAsync<AlreadyExistsException>(() =>
            handler.Handle(new CreateClientScopeCommand { ClientId = clientId, ScopeId = scopeId }, CancellationToken.None));
    }

    [Fact]
    public async Task CreateOperation_persists_and_maps_its_name_in_the_scope_cache()
    {
        using var host = new InMemoryHandlerHost();
        var handler = new CreateOperationCommandHandler(host.Uow, host.ScopeCache);

        var id = await handler.Handle(
            new CreateOperationCommand { Name = "read:reports", Description = "Read reports." }, CancellationToken.None);

        Assert.True(await host.Context.Set<Operation>().AnyAsync(x => x.Id == id));
        Assert.Equal(id, await host.ScopeCache.GetOperationIdByNameAsync("read:reports")); // name→id write-through

        await Assert.ThrowsAsync<AlreadyExistsException>(() =>
            handler.Handle(new CreateOperationCommand { Name = "read:reports", Description = "dup" }, CancellationToken.None));
    }

    [Fact]
    public async Task CreateUser_persists_the_user_with_its_roles()
    {
        using var host = new InMemoryHandlerHost();

        var role = Role.Create($"role-{Guid.NewGuid():N}");
        await host.Context.Set<Role>().AddAsync(role);
        await host.Context.SaveChangesAsync();

        var handler = new CreateUserCommandHandler(host.Uow);
        var email = $"{Guid.NewGuid():N}@test.local";
        var userId = await handler.Handle(new CreateUserCommand
        {
            Username = "unit-user",
            Email = email,
            Password = "Secret123!",
            UserRoles = new[] { new CreateUserRoleCommand { RoleId = role.Id } },
        }, CancellationToken.None);

        Assert.True(await host.Context.Set<User>().AnyAsync(u => u.Id == userId));
        Assert.True(await host.Context.Set<UserRole>().AnyAsync(x => x.UserId == userId && x.RoleId == role.Id));
    }

    [Fact]
    public async Task CreateUser_rejects_a_duplicate_email()
    {
        using var host = new InMemoryHandlerHost();
        var handler = new CreateUserCommandHandler(host.Uow);
        var email = $"{Guid.NewGuid():N}@test.local";

        await handler.Handle(new CreateUserCommand
        {
            Username = "first", Email = email, Password = "Secret123!",
        }, CancellationToken.None);

        await Assert.ThrowsAsync<AlreadyExistsException>(() => handler.Handle(new CreateUserCommand
        {
            Username = "second", Email = email, Password = "Secret123!",
        }, CancellationToken.None));
    }

    private static async Task<(Guid ClientId, Guid ScopeId)> SeedClientAndScopeAsync(InMemoryHandlerHost host)
    {
        // ClientScope has FKs to both Clients and Scopes — create the referenced rows first.
        var client = Client.Create($"client-{Guid.NewGuid():N}");
        var (secretHash, secretSalt) = PasswordHasher.Hash("client-secret");
        client.UpdateSecret(secretHash, secretSalt);
        var scope = Scope.Create($"scope-{Guid.NewGuid():N}");

        await host.Context.Set<Client>().AddAsync(client);
        await host.Context.Set<Scope>().AddAsync(scope);
        await host.Context.SaveChangesAsync();
        return (client.Id, scope.Id);
    }
}
