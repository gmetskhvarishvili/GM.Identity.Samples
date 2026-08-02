using GM.EntityFramework.Domain.Exceptions;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.ClientScopeAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.OperationAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.PermissionAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.RoleAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.RolePermissionAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.ScopeAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.ScopeOperationAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.UserRoleAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.ClientSessionAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.UserSessionAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.ClientAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.TwoFactorAuthTypeAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserTwoFactorAuthTypeAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.MessagingBoundedContext.OutboxMessageAggregate.Interfaces;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Identity.Sample.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace GM.Identity.Sample.Persistence.UnitOfWork;

public class UnitOfWork(
    ApplicationDbContext context,
    IOutboxMessageRepository outboxMessageRepository,
    IClientRepository clientRepository,
    IClientSessionRepository clientSessionRepository,
    IClientScopeRepository clientScopeRepository,
    IScopeRepository scopeRepository,
    IScopeOperationRepository scopeOperationRepository,
    IOperationRepository operationRepository,
    IUserSessionRepository userSessionRepository,
    IUserRepository userRepository,
    IUserRoleRepository userRoleRepository,
    IUserTwoFactorAuthTypeRepository userTwoFactorAuthTypeRepository,
    ITwoFactorAuthTypeRepository twoFactorAuthTypeRepository,
    IRoleRepository roleRepository,
    IRolePermissionRepository rolePermissionRepository,
    IPermissionRepository permissionRepository)
    : IUnitOfWork
{
    private IDbContextTransaction? _transaction;
    public IOutboxMessageRepository OutboxMessageRepository { get; } = outboxMessageRepository;
    public IClientRepository ClientRepository { get; } = clientRepository;
    public IClientSessionRepository ClientSessionRepository { get; } = clientSessionRepository;
    public IClientScopeRepository ClientScopeRepository { get; } = clientScopeRepository;
    public IScopeRepository ScopeRepository { get; } = scopeRepository;
    public IScopeOperationRepository ScopeOperationRepository { get; } = scopeOperationRepository;
    public IOperationRepository OperationRepository { get; } = operationRepository;
    public IUserSessionRepository UserSessionRepository { get; } = userSessionRepository;
    public IUserTwoFactorAuthTypeRepository UserTwoFactorAuthTypeRepository { get; } = userTwoFactorAuthTypeRepository;
    public ITwoFactorAuthTypeRepository TwoFactorAuthTypeRepository { get; } = twoFactorAuthTypeRepository;
    public IUserRepository UserRepository { get; } = userRepository;
    public IUserRoleRepository UserRoleRepository { get; } = userRoleRepository;
    public IRoleRepository RoleRepository { get; } = roleRepository;
    public IRolePermissionRepository RolePermissionRepository { get; } = rolePermissionRepository;
    public IPermissionRepository PermissionRepository { get; } = permissionRepository;
    
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyException("A concurrency error occurred.", ex);
        }
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction ??= await context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await SaveChangesAsync(cancellationToken);
            await _transaction?.CommitAsync(cancellationToken)!;
        }
        catch
        {
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
        finally
        {
            if (_transaction != null)
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default)
    {
        await BeginTransactionAsync(cancellationToken);
        try
        {
            await operation();
            await CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        context.Dispose();
    }
}