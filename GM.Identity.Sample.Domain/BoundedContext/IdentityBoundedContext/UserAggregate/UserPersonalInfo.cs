using GM.Identity.Domain.Identity.UserPersonalInfoAggregate.Entities;

using System;

namespace GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserAggregate;

/// <summary>
/// The sample's concrete personal-info entity. Its shape and behaviour live in the GM.Identity base
/// <see cref="GMUserPersonalInfo"/>; it is owned 1:1 by <see cref="User"/> (no separate aggregate root) and
/// persisted with it, mirroring how the other sample entities specialise their GM.Identity bases.
/// </summary>
public class UserPersonalInfo : GMUserPersonalInfo
{
    private UserPersonalInfo() { } // EF Core materialization

    private UserPersonalInfo(
        Guid userId, string? firstName, string? lastName, string? personalNumber, DateTime? birthDate)
        : base(userId, firstName, lastName, personalNumber, birthDate) { }

    public static UserPersonalInfo Create(
        Guid userId, string? firstName, string? lastName, string? personalNumber, DateTime? birthDate) =>
        new(userId, firstName, lastName, personalNumber, birthDate);
}
