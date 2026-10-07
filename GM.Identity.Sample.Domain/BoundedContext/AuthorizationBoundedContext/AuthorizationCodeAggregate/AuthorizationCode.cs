using GM.EntityFramework.Domain.Abstractions;
using GM.Identity.Domain.Authorization.AuthorizationCodeAggregate.Entities;

using System;

namespace GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.AuthorizationCodeAggregate;

/// <summary>
/// The sample's concrete OAuth authorization-code aggregate root. Its shape and behaviour live in the
/// GM.Identity base <see cref="GMAuthorizationCode"/>; this type fixes it as the application's aggregate and
/// exposes the factory, mirroring how the other sample entities specialise their GM.Identity bases.
/// </summary>
public class AuthorizationCode : GMAuthorizationCode, IAggregateRoot
{
    private AuthorizationCode() { } // EF Core materialization

    private AuthorizationCode(
        Guid clientId, Guid userId, string codeHash, string redirectUri,
        string? scope, string codeChallenge, string codeChallengeMethod, DateTime expiresAt, Guid? ssoSessionId,
        string? nonce)
        : base(clientId, userId, codeHash, redirectUri, scope, codeChallenge, codeChallengeMethod, expiresAt,
            ssoSessionId, nonce) { }

    public static AuthorizationCode Create(
        Guid clientId, Guid userId, string codeHash, string redirectUri,
        string? scope, string codeChallenge, string codeChallengeMethod, DateTime expiresAt,
        Guid? ssoSessionId = null, string? nonce = null) =>
        new(clientId, userId, codeHash, redirectUri, scope, codeChallenge, codeChallengeMethod, expiresAt,
            ssoSessionId, nonce);
}
