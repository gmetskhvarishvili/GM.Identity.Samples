using GM.EntityFramework.Persistence;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.ClientScopeAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.OperationAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.PermissionAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.RoleAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.GroupAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.RolePermissionAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.ScopeAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.ScopeOperationAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.UserPermissionAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.UserRoleAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.AuthorizationCodeAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.ClientSessionAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.DeviceCodeAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.SsoSessionAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.UserClientConsentAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.UserSessionAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.ApiKeyAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.ClientAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.ClientRedirectUriAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.PasskeyAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserConsentAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserPasswordHistoryAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserPendingContactChangeAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserTotpDeviceAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.TwoFactorAuthTypeAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserRecoveryCodeAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserTwoFactorAuthTypeAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.MessagingBoundedContext.EmailTemplateAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.MessagingBoundedContext.OutboxMessageAggregate.Interfaces;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Identity.Sample.Persistence.Context;

namespace GM.Identity.Sample.Persistence.UnitOfWork;

public sealed class UnitOfWork(
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
    IUserPermissionRepository userPermissionRepository,
    IUserTwoFactorAuthTypeRepository userTwoFactorAuthTypeRepository,
    IUserRecoveryCodeRepository userRecoveryCodeRepository,
    IUserPasswordHistoryRepository userPasswordHistoryRepository,
    IUserPendingContactChangeRepository userPendingContactChangeRepository,
    IUserTotpDeviceRepository userTotpDeviceRepository,
    IClientRedirectUriRepository clientRedirectUriRepository,
    IAuthorizationCodeRepository authorizationCodeRepository,
    ISsoSessionRepository ssoSessionRepository,
    IUserClientConsentRepository userClientConsentRepository,
    IDeviceCodeRepository deviceCodeRepository,
    IApiKeyRepository apiKeyRepository,
    IUserConsentRepository userConsentRepository,
    IUserPasskeyRepository userPasskeyRepository,
    IPasskeyChallengeRepository passkeyChallengeRepository,
    IEmailTemplateRepository emailTemplateRepository,
    ITwoFactorAuthTypeRepository twoFactorAuthTypeRepository,
    IRoleRepository roleRepository,
    IGroupRepository groupRepository,
    IGroupRoleRepository groupRoleRepository,
    IUserGroupRepository userGroupRepository,
    IRolePermissionRepository rolePermissionRepository,
    IPermissionRepository permissionRepository)
    : GenericUnitOfWork<ApplicationDbContext>(context), IUnitOfWork
{
    public IOutboxMessageRepository OutboxMessageRepository { get; } = outboxMessageRepository;
    public IClientRepository ClientRepository { get; } = clientRepository;
    public IClientSessionRepository ClientSessionRepository { get; } = clientSessionRepository;
    public IClientScopeRepository ClientScopeRepository { get; } = clientScopeRepository;
    public IScopeRepository ScopeRepository { get; } = scopeRepository;
    public IScopeOperationRepository ScopeOperationRepository { get; } = scopeOperationRepository;
    public IOperationRepository OperationRepository { get; } = operationRepository;
    public IUserSessionRepository UserSessionRepository { get; } = userSessionRepository;
    public IUserTwoFactorAuthTypeRepository UserTwoFactorAuthTypeRepository { get; } = userTwoFactorAuthTypeRepository;
    public IUserRecoveryCodeRepository UserRecoveryCodeRepository { get; } = userRecoveryCodeRepository;
    public IUserPasswordHistoryRepository UserPasswordHistoryRepository { get; } = userPasswordHistoryRepository;
    public IUserPendingContactChangeRepository UserPendingContactChangeRepository { get; } = userPendingContactChangeRepository;
    public IUserTotpDeviceRepository UserTotpDeviceRepository { get; } = userTotpDeviceRepository;
    public IClientRedirectUriRepository ClientRedirectUriRepository { get; } = clientRedirectUriRepository;
    public IAuthorizationCodeRepository AuthorizationCodeRepository { get; } = authorizationCodeRepository;
    public ISsoSessionRepository SsoSessionRepository { get; } = ssoSessionRepository;
    public IUserClientConsentRepository UserClientConsentRepository { get; } = userClientConsentRepository;
    public IDeviceCodeRepository DeviceCodeRepository { get; } = deviceCodeRepository;
    public IApiKeyRepository ApiKeyRepository { get; } = apiKeyRepository;
    public IUserConsentRepository UserConsentRepository { get; } = userConsentRepository;
    public IUserPasskeyRepository UserPasskeyRepository { get; } = userPasskeyRepository;
    public IPasskeyChallengeRepository PasskeyChallengeRepository { get; } = passkeyChallengeRepository;
    public IEmailTemplateRepository EmailTemplateRepository { get; } = emailTemplateRepository;
    public ITwoFactorAuthTypeRepository TwoFactorAuthTypeRepository { get; } = twoFactorAuthTypeRepository;
    public IUserRepository UserRepository { get; } = userRepository;
    public IUserRoleRepository UserRoleRepository { get; } = userRoleRepository;
    public IUserPermissionRepository UserPermissionRepository { get; } = userPermissionRepository;
    public IRoleRepository RoleRepository { get; } = roleRepository;
    public IGroupRepository GroupRepository { get; } = groupRepository;
    public IGroupRoleRepository GroupRoleRepository { get; } = groupRoleRepository;
    public IUserGroupRepository UserGroupRepository { get; } = userGroupRepository;
    public IRolePermissionRepository RolePermissionRepository { get; } = rolePermissionRepository;
    public IPermissionRepository PermissionRepository { get; } = permissionRepository;
}
