using GM.EntityFramework.Domain.Abstractions;
using GM.Identity.Domain.Authorization.DeviceCodeAggregate.Entities;

using System;

namespace GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.DeviceCodeAggregate;

/// <summary>
/// The sample's concrete Device Authorization Grant (RFC 8628) aggregate root. Its shape and behaviour live in
/// the GM.Identity base <see cref="GMDeviceCode"/> (as does <see cref="DeviceCodeStatus"/>); this type fixes it
/// as the application's aggregate and exposes the factory, mirroring the other sample entities.
/// </summary>
public class DeviceCode : GMDeviceCode, IAggregateRoot
{
    private DeviceCode() { } // EF Core materialization

    private DeviceCode(
        Guid clientId, string deviceCodeHash, string userCode, int intervalSeconds, DateTime expiresAt)
        : base(clientId, deviceCodeHash, userCode, intervalSeconds, expiresAt) { }

    public static DeviceCode Create(
        Guid clientId, string deviceCodeHash, string userCode, int intervalSeconds, DateTime expiresAt) =>
        new(clientId, deviceCodeHash, userCode, intervalSeconds, expiresAt);
}
