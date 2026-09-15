using GM.EntityFramework.Domain.Abstractions;
using GM.EntityFramework.Domain.Base;
using GM.Identity.Sample.Domain.Enums;

using System;

namespace GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserPendingContactChangeAggregate;

/// <summary>
/// A requested-but-not-yet-verified change to a user's email or phone. The new contact is only written onto the
/// user once a one-time code sent to that new contact is confirmed — so a user can't set an address they don't
/// control, and a typo can't lock them out.
/// </summary>
public class UserPendingContactChange : SoftDeletableEntity<Guid>, IAggregateRoot
{
    private UserPendingContactChange() // EF Core materialization
    {
    }

    private UserPendingContactChange(Guid userId, ConfirmationType confirmationType, string newContact)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        ConfirmationType = confirmationType;
        NewContact = newContact;
    }

    public static UserPendingContactChange Create(Guid userId, ConfirmationType confirmationType, string newContact) =>
        new(userId, confirmationType, newContact);

    public Guid UserId { get; private set; }

    /// <summary>Whether this changes the email or the phone number (and which channel the code was sent over).</summary>
    public ConfirmationType ConfirmationType { get; private set; }

    /// <summary>The new email address or phone number awaiting confirmation.</summary>
    public string NewContact { get; private set; } = null!;
}
