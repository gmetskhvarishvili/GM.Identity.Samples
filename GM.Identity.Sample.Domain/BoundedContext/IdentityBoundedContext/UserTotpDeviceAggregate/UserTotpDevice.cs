using GM.EntityFramework.Domain.Abstractions;
using GM.EntityFramework.Domain.Base;

using System;

namespace GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserTotpDeviceAggregate;

/// <summary>
/// An authenticator-app (TOTP) enrolment for a user: the shared secret plus its confirmation state. Starts
/// unconfirmed — only once the user proves they can produce a code (confirm) does it gate login. The secret is
/// stored as Base32 (a production system should additionally encrypt it at rest).
/// </summary>
public class UserTotpDevice : SoftDeletableEntity<Guid>, IAggregateRoot
{
    private UserTotpDevice() // EF Core materialization
    {
    }

    private UserTotpDevice(Guid userId, string secretBase32)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        SecretBase32 = secretBase32;
    }

    public static UserTotpDevice Create(Guid userId, string secretBase32) => new(userId, secretBase32);

    public Guid UserId { get; private set; }
    public string SecretBase32 { get; private set; } = null!;
    public bool IsConfirmed { get; private set; }
    public DateTime? ConfirmedAt { get; private set; }

    /// <summary>Marks the device confirmed (idempotent). The caller verifies a code before calling this.</summary>
    public void Confirm()
    {
        if (IsConfirmed) return;
        IsConfirmed = true;
        ConfirmedAt = DateTime.UtcNow;
    }
}
