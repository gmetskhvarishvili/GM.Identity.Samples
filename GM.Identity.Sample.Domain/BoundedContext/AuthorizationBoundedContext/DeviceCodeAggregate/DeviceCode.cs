using GM.EntityFramework.Domain.Abstractions;
using GM.EntityFramework.Domain.Base;

using System;

namespace GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.DeviceCodeAggregate;

/// <summary>The lifecycle of a device-authorization request.</summary>
public enum DeviceCodeStatus
{
    /// <summary>Issued, awaiting the user's approval at the verification URI.</summary>
    Pending = 0,

    /// <summary>The user approved; the device may now exchange the device_code for tokens.</summary>
    Approved = 1,

    /// <summary>The user declined; the device must stop polling.</summary>
    Denied = 2,
}

/// <summary>
/// A Device Authorization Grant request (RFC 8628). The browserless device polls the token endpoint with the
/// device_code while the user approves the paired, human-readable user_code on another device. Only the device
/// code's hash is stored; the poll interval is enforced to throttle clients.
/// </summary>
public class DeviceCode : SoftDeletableEntity<Guid>, IAggregateRoot
{
    private DeviceCode() { } // EF Core materialization

    private DeviceCode(
        Guid clientId, string deviceCodeHash, string userCode, int intervalSeconds, DateTime expiresAt)
    {
        Id = Guid.NewGuid();
        ClientId = clientId;
        DeviceCodeHash = deviceCodeHash;
        UserCode = userCode;
        IntervalSeconds = intervalSeconds;
        ExpiresAt = expiresAt;
        Status = DeviceCodeStatus.Pending;
    }

    public static DeviceCode Create(
        Guid clientId, string deviceCodeHash, string userCode, int intervalSeconds, DateTime expiresAt) =>
        new(clientId, deviceCodeHash, userCode, intervalSeconds, expiresAt);

    public Guid ClientId { get; private set; }
    public string DeviceCodeHash { get; private set; } = null!;

    /// <summary>The short, human-readable code the user enters at the verification URI (normalized, uppercase).</summary>
    public string UserCode { get; private set; } = null!;

    public int IntervalSeconds { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DeviceCodeStatus Status { get; private set; }

    /// <summary>The approving user, set when the request is approved.</summary>
    public Guid? UserId { get; private set; }

    /// <summary>When the device last polled — used to enforce the minimum interval (slow_down).</summary>
    public DateTime? LastPolledAt { get; private set; }

    public bool IsExpired(DateTime now) => ExpiresAt <= now;

    /// <summary>Records a poll and reports whether it arrived sooner than the allowed interval (→ slow_down).</summary>
    public bool RegisterPollAndCheckTooFast(DateTime now)
    {
        var tooFast = LastPolledAt is { } last && (now - last) < TimeSpan.FromSeconds(IntervalSeconds);
        LastPolledAt = now;
        return tooFast;
    }

    /// <summary>Approves the request for a user (only from Pending).</summary>
    public bool Approve(Guid userId)
    {
        if (Status != DeviceCodeStatus.Pending) return false;
        Status = DeviceCodeStatus.Approved;
        UserId = userId;
        return true;
    }

    /// <summary>Declines the request (only from Pending).</summary>
    public bool Deny()
    {
        if (Status != DeviceCodeStatus.Pending) return false;
        Status = DeviceCodeStatus.Denied;
        return true;
    }
}
