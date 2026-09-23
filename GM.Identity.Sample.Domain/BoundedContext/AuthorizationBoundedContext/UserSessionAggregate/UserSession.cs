using GM.EntityFramework.Domain.Abstractions;
using GM.EntityFramework.Domain.Common;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.ClientScopeAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.OperationAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.PermissionAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.RoleAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.RolePermissionAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.ScopeAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.ScopeOperationAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.UserRoleAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.ClientSessionAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.ClientAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.TwoFactorAuthTypeAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserTwoFactorAuthTypeAggregate;
using GM.Identity.Domain.Authorization.UserSessionAggregate.Entities;

using System;
namespace GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.UserSessionAggregate;

public class UserSession : GMUserSession
<Client, ClientSession, ClientScope, Scope, ScopeOperation, Operation,
    User, UserSession, TwoFactorAuthType, UserTwoFactorAuthType,
    UserRole, Role, RolePermission, Permission>, IAggregateRoot, ICapturesActorContext
{
    // Request/tracing context under which the session was created â€” snapshotted on insert by the GM
    // ActorAuditInterceptor (from ICurrentActor/IEventSource), the same context stamped on domain events.
    // UserId/ClientId are set by the login flow via the base; these capture "where/how" the session began.
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public string? ChannelId { get; private set; }
    public string? Culture { get; private set; }
    public string? CorrelationId { get; private set; }
    public string? IdempotencyKey { get; private set; }
    public Guid? TenantId { get; private set; }
    public string? Source { get; private set; }

    /// <summary>Hash of the refresh token that can renew this session's access token (null for none).</summary>
    public string? RefreshTokenHash { get; private set; }

    /// <summary>
    /// The single sign-on session this app session was established through, when the login went via the SSO
    /// browser session at <c>/connect/authorize</c>. Null for sessions minted by non-SSO grants (password,
    /// client-credentials, passkey). Ending the SSO session revokes every session that carries its id.
    /// </summary>
    public Guid? SsoSessionId { get; private set; }

    private UserSession() // EF Core materialization
    {
    }

    private UserSession(
        Guid? userId,
        Guid? clientId,
        string? provider,
        string tokenHash,
        string? refreshTokenHash,
        DateTime expiresAt,
        Guid? ssoSessionId) :
        base(
            userId,
            clientId,
            provider,
            tokenHash,
            expiresAt)
    {
        RefreshTokenHash = refreshTokenHash;
        SsoSessionId = ssoSessionId;
    }

    public static UserSession Create(
        Guid? userId,
        Guid? clientId,
        string? provider,
        string tokenHash,
        DateTime expiresAt,
        string? refreshTokenHash = null,
        Guid? ssoSessionId = null)
    {
        return new UserSession(userId, clientId, provider, tokenHash, refreshTokenHash, expiresAt, ssoSessionId);
    }
}
