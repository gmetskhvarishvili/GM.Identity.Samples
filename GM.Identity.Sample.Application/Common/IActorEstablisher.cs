using System;

namespace GM.Identity.Sample.Application.Common;

/// <summary>
/// Records who the caller turned out to be during an anonymous request. The audit trail (the domain-event
/// actor columns, <c>CreatedBy</c>/<c>UpdatedBy</c>) is stamped from the current actor, which normally comes
/// from the gateway's <c>X-User-Id</c> / <c>X-Client-Id</c> / <c>X-Session-Id</c> headers — but a login or
/// sign-up arrives without them. A handler that has just authenticated (or created) the user calls this
/// before saving, so the events of that request name the user, client and session instead of nobody.
/// Values only fill what the request did not already carry: an authenticated caller (e.g. an admin creating a
/// user) stays the actor. Implemented in the API; a no-op outside an HTTP request.
/// </summary>
public interface IActorEstablisher
{
    /// <summary>Sets the request's user, client and session where the headers left them empty.</summary>
    void Establish(Guid? userId, Guid? clientId, Guid? sessionId);
}
