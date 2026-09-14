using GM.Identity.Authorization;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.ClientScopeAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.OperationAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.PermissionAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.RoleAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.RolePermissionAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.ScopeAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.ScopeOperationAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.UserRoleAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.ClientAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserAggregate;
using GM.Identity.Sample.Domain.SeedWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Persistence.Context;

public class ApplicationDbContextSeed
{
    private const string AdminRoleName = "Administrator";
    private const string AdminUserName = "admin";
    private const string AdminEmail = "admin@local";

    private const string DefaultClientName = "sample-client";

    // Dev-only fallbacks. Real deployments override via configuration/secrets (Seed:*), so known
    // credentials are never baked into a shipped build.
    private const string DefaultDevAdminPassword = "Admin123!";
    private const string DefaultDevClientSecret = "secret";
    private static readonly Guid DefaultClientId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    /// <summary>
    /// Seeds RBAC reference data so the <c>[HasPermission]</c>-gated actions are usable. <paramref name="permissionNames"/>
    /// are discovered from the controller attributes by the caller (one permission per gated action, name = action
    /// name). Idempotent ("if not exists"): upserts each permission, an <c>Administrator</c> role granted every
    /// permission, and an <c>admin</c> user holding that role — into the database and, when a
    /// <paramref name="permissionCache"/> is supplied, into the Redis RBAC projection (name→id map, role→permissions,
    /// user→roles).
    /// </summary>
    public async Task SeedAsync(
        ApplicationDbContext context,
        ILogger<ApplicationDbContextSeed> logger,
        IReadOnlyCollection<string>? permissionNames = null,
        IPermissionCache? permissionCache = null,
        string? adminPassword = null,
        Guid? clientId = null,
        string? clientSecret = null,
        IScopeCache? scopeCache = null,
        int? retry = 0,
        CancellationToken cancellationToken = default)
    {
        int retryForAvailability = retry ?? 0;

        try
        {
            if (permissionNames is null || permissionNames.Count == 0) return;

            // 1) Permissions (name = action name) — create any that don't exist.
            var nameToId = (await context.Set<Permission>().AsNoTracking().ToListAsync(cancellationToken))
                .Where(p => p.Name is not null)
                .GroupBy(p => p.Name!)
                .ToDictionary(g => g.Key, g => g.First().Id);

            var createdPermissions = false;
            foreach (var name in permissionNames.Where(n => !nameToId.ContainsKey(n)))
            {
                var permission = Permission.Create(name, name);
                await context.Set<Permission>().AddAsync(permission, cancellationToken);
                nameToId[name] = permission.Id;
                createdPermissions = true;
            }
            if (createdPermissions) await context.SaveChangesAsync(cancellationToken);

            var allPermissionIds = permissionNames.Select(n => nameToId[n]).ToList();

            // 2) Administrator role.
            var adminRole = await context.Set<Role>()
                .FirstOrDefaultAsync(r => r.Name == AdminRoleName && !r.IsDeleted, cancellationToken);
            if (adminRole is null)
            {
                adminRole = Role.Create(AdminRoleName);
                await context.Set<Role>().AddAsync(adminRole, cancellationToken);
                await context.SaveChangesAsync(cancellationToken);
            }

            // 3) Grant every permission to the Administrator role (missing links only).
            var existingRolePermissions = (await context.Set<RolePermission>()
                    .Where(rp => rp.RoleId == adminRole.Id && !rp.IsDeleted)
                    .Select(rp => rp.PermissionId)
                    .ToListAsync(cancellationToken))
                .ToHashSet();
            var createdLinks = false;
            foreach (var permissionId in allPermissionIds.Where(id => !existingRolePermissions.Contains(id)))
            {
                await context.Set<RolePermission>().AddAsync(RolePermission.Create(adminRole.Id, permissionId), cancellationToken);
                createdLinks = true;
            }
            if (createdLinks) await context.SaveChangesAsync(cancellationToken);

            // 4) Admin user holding the Administrator role.
            var adminUser = await context.Set<User>()
                .FirstOrDefaultAsync(u => u.UserName == AdminUserName && !u.IsDeleted, cancellationToken);
            if (adminUser is null)
            {
                adminUser = User.Create(AdminUserName, AdminEmail, null);
                var (hash, salt) = PasswordHasher.Hash(
                    string.IsNullOrWhiteSpace(adminPassword) ? DefaultDevAdminPassword : adminPassword);
                adminUser.UpdatePassword(hash, salt);
                await context.Set<User>().AddAsync(adminUser, cancellationToken);
                await context.SaveChangesAsync(cancellationToken);
            }

            var hasAdminRole = await context.Set<UserRole>()
                .AnyAsync(ur => ur.UserId == adminUser.Id && ur.RoleId == adminRole.Id && !ur.IsDeleted, cancellationToken);
            if (!hasAdminRole)
            {
                await context.Set<UserRole>().AddAsync(UserRole.Create(adminUser.Id, adminRole.Id), cancellationToken);
                await context.SaveChangesAsync(cancellationToken);
            }

            // 4b) OAuth client the token endpoint requires (fixed id/secret so the sample is usable and
            // tests are deterministic; override via Seed:ClientId / Seed:ClientSecret).
            var seedClientId = clientId ?? DefaultClientId;
            var clientExists = await context.Set<Client>().AnyAsync(c => c.Id == seedClientId, cancellationToken);
            if (!clientExists)
            {
                var client = Client.Create(DefaultClientName);
                client.Id = seedClientId;
                var (secretHash, secretSalt) = PasswordHasher.Hash(
                    string.IsNullOrWhiteSpace(clientSecret) ? DefaultDevClientSecret : clientSecret);
                client.UpdateSecret(secretHash, secretSalt);
                await context.Set<Client>().AddAsync(client, cancellationToken);
                await context.SaveChangesAsync(cancellationToken);
            }

            // 5) Mirror RBAC into Redis (name→id map + the admin's role/permission sets). Idempotent.
            if (permissionCache is not null)
            {
                foreach (var (name, id) in nameToId.Where(kv => permissionNames.Contains(kv.Key)))
                    await permissionCache.SetPermissionIdAsync(name, id, cancellationToken);
                await permissionCache.ReplaceRolePermissionsAsync(adminRole.Id, allPermissionIds, cancellationToken);
                await permissionCache.AddUserRoleAsync(adminUser.Id, adminRole.Id, cancellationToken);
            }

            // 6) Scope-based-authorization demo graph: two operations (read + manage), each in its own
            // scope, both granted to the default client — so the [RequiresScope]-gated endpoints (read on
            // GETs, manage on writes) across all controllers are callable with a token from the seeded
            // client (Client → Scope → Operation). Mirrored to the Redis scope projection.
            var scopeGraph = new[]
            {
                (Operation: ScopeOperations.ReadIdentity, Scope: ScopeOperations.IdentityReadScope,
                    Description: "Read the identity-management API."),
                (Operation: ScopeOperations.ManageIdentity, Scope: ScopeOperations.IdentityManageScope,
                    Description: "Write to the identity-management API."),
            };

            foreach (var (operationName, scopeName, description) in scopeGraph)
            {
                var operation = await context.Set<Operation>()
                    .FirstOrDefaultAsync(o => o.Name == operationName && !o.IsDeleted, cancellationToken);
                if (operation is null)
                {
                    operation = Operation.Create(operationName, description);
                    await context.Set<Operation>().AddAsync(operation, cancellationToken);
                    await context.SaveChangesAsync(cancellationToken);
                }

                var scope = await context.Set<Scope>()
                    .FirstOrDefaultAsync(s => s.Name == scopeName && !s.IsDeleted, cancellationToken);
                if (scope is null)
                {
                    scope = Scope.Create(scopeName);
                    await context.Set<Scope>().AddAsync(scope, cancellationToken);
                    await context.SaveChangesAsync(cancellationToken);
                }

                if (!await context.Set<ScopeOperation>().AnyAsync(
                        so => so.ScopeId == scope.Id && so.OperationId == operation.Id && !so.IsDeleted, cancellationToken))
                {
                    await context.Set<ScopeOperation>().AddAsync(ScopeOperation.Create(scope.Id, operation.Id), cancellationToken);
                    await context.SaveChangesAsync(cancellationToken);
                }

                if (!await context.Set<ClientScope>().AnyAsync(
                        cs => cs.ClientId == seedClientId && cs.ScopeId == scope.Id && !cs.IsDeleted, cancellationToken))
                {
                    await context.Set<ClientScope>().AddAsync(ClientScope.Create(seedClientId, scope.Id), cancellationToken);
                    await context.SaveChangesAsync(cancellationToken);
                }

                if (scopeCache is not null)
                {
                    await scopeCache.SetOperationIdAsync(operation.Name!, operation.Id, cancellationToken);
                    await scopeCache.AddScopeOperationAsync(scope.Id, operation.Id, cancellationToken);
                    await scopeCache.AddClientScopeAsync(seedClientId, scope.Id, cancellationToken);
                }
            }

            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation(
                    "RBAC seed complete: {PermissionCount} permissions, Administrator role, admin user '{AdminUser}', client '{ClientId}'.",
                    allPermissionIds.Count, AdminUserName, seedClientId);
        }
        catch (Exception ex)
        {
            if (retryForAvailability < 10)
            {
                retryForAvailability++;
                logger.LogError(ex, "EXCEPTION ERROR while seeding {DbContextName}", nameof(ApplicationDbContext));
                await SeedAsync(context, logger, permissionNames, permissionCache, adminPassword, clientId, clientSecret, scopeCache, retryForAvailability, cancellationToken);
            }
        }
    }
}
