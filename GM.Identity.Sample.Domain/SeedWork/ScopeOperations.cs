namespace GM.Identity.Sample.Domain.SeedWork;

/// <summary>
/// Well-known operation and scope names for the seeded scope-based-authorization demonstration. Shared so
/// the seeder (which provisions them), the <c>[RequiresScope]</c> attribute (which requires them) and the
/// tests all agree on the exact strings.
/// </summary>
public static class ScopeOperations
{
    /// <summary>Operation guarding read (GET) access to the identity-management API.</summary>
    public const string ReadIdentity = "read:identity";

    /// <summary>Operation guarding write (POST/PUT/DELETE) access to the identity-management API.</summary>
    public const string ManageIdentity = "manage:identity";

    /// <summary>The seeded scope that covers <see cref="ReadIdentity"/>, granted to the default client.</summary>
    public const string IdentityReadScope = "identity.read";

    /// <summary>The seeded scope that covers <see cref="ManageIdentity"/>, granted to the default client.</summary>
    public const string IdentityManageScope = "identity.manage";
}
