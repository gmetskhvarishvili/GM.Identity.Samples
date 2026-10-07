using GM.EntityFramework.Domain.Abstractions;
using GM.OTP.Domain.Entities;

using System;

namespace GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserTotpDeviceAggregate;

/// <summary>
/// The sample's concrete authenticator-app (TOTP) enrolment. Its shape and behaviour live in the GM.OTP base
/// <see cref="TotpEnrolment"/> — consolidating all one-time-password concerns in GM.OTP — while this type fixes
/// it as the application's aggregate and exposes the factory. Codes are verified with GM.OTP's
/// <c>ITotpCodeService</c> (RFC 6238).
/// </summary>
public class UserTotpDevice : TotpEnrolment, IAggregateRoot
{
    private UserTotpDevice() // EF Core materialization
    {
    }

    private UserTotpDevice(string subject, string secretBase32, Guid userId)
        : base(subject, secretBase32, userId)
    {
    }

    public static UserTotpDevice Create(Guid userId, string subject, string secretBase32) =>
        new(subject, secretBase32, userId);
}
