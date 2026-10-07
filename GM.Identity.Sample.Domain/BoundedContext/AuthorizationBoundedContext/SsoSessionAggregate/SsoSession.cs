using GM.EntityFramework.Domain.Abstractions;
using GM.Identity.Domain.Authorization.SsoSessionAggregate.Entities;

using System;

namespace GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.SsoSessionAggregate;

/// <summary>
/// The sample's concrete single sign-on session aggregate root. Its shape and behaviour live in the GM.Identity
/// base <see cref="GMSsoSession"/>; this type fixes it as the application's aggregate and exposes the factory,
/// mirroring how the other sample entities specialise their GM.Identity bases.
/// </summary>
public class SsoSession : GMSsoSession, IAggregateRoot
{
    private SsoSession() { } // EF Core materialization

    private SsoSession(
        Guid userId, string tokenHash, DateTime authTime, DateTime expiresAt, string? ipAddress, string? userAgent)
        : base(userId, tokenHash, authTime, expiresAt, ipAddress, userAgent) { }

    public static SsoSession Create(
        Guid userId, string tokenHash, DateTime authTime, DateTime expiresAt,
        string? ipAddress = null, string? userAgent = null) =>
        new(userId, tokenHash, authTime, expiresAt, ipAddress, userAgent);
}
