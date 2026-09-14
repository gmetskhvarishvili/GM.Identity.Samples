using GM.EntityFramework.Domain.Abstractions;
using GM.EntityFramework.Domain.Base;

using System;

namespace GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.ClientRedirectUriAggregate;

/// <summary>
/// A redirect URI registered for an OAuth client. The authorization-code flow will only redirect back to a URI
/// that exactly matches one registered here, so an attacker cannot substitute their own callback.
/// </summary>
public class ClientRedirectUri : SoftDeletableEntity<Guid>, IAggregateRoot
{
    private ClientRedirectUri() // EF Core materialization
    {
    }

    private ClientRedirectUri(Guid clientId, string uri)
    {
        Id = Guid.NewGuid();
        ClientId = clientId;
        Uri = uri;
    }

    public static ClientRedirectUri Create(Guid clientId, string uri) => new(clientId, uri);

    public Guid ClientId { get; private set; }

    /// <summary>The exact redirect URI (compared verbatim during the authorization flow).</summary>
    public string Uri { get; private set; } = null!;
}
