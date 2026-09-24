namespace GM.Identity.Sample.Application.Users.Commands.CreateUser;

/// <summary>
/// A consent the new user is accepting at registration: the document type and the exact version accepted. Recorded
/// as a <see cref="Domain.BoundedContext.IdentityBoundedContext.UserConsentAggregate.UserConsent"/> audit row.
/// </summary>
public class CreateUserConsentInput
{
    /// <summary>The consent document accepted (e.g. <c>TermsOfService</c>, <c>PrivacyPolicy</c>).</summary>
    public string ConsentType { get; set; } = null!;

    /// <summary>The version of the document being accepted; must match the document's current version.</summary>
    public string DocumentVersion { get; set; } = null!;
}
