using GM.Identity.Persistence.Configuration;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.PasskeyAggregate;

namespace GM.Identity.Sample.Persistence.Configuration;

// Both passkey mappings live in GM.Identity base configs; these fix them to the sample's aggregates
// (named by IdentityTableNames, without GM.Identity's gm_ prefix).
public class UserPasskeyConfiguration : GMUserPasskeyConfiguration<UserPasskey>
{
    protected override string Schema => IdentityTableNames.Schemas.Identity;
    protected override string Table => IdentityTableNames.UserPasskeys;
}

public class PasskeyChallengeConfiguration : GMPasskeyChallengeConfiguration<PasskeyChallenge>
{
    protected override string Schema => IdentityTableNames.Schemas.Identity;
    protected override string Table => IdentityTableNames.PasskeyChallenges;
}
