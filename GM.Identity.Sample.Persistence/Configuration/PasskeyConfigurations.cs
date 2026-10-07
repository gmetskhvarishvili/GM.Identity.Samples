using GM.Identity.Persistence.Configuration;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.PasskeyAggregate;

namespace GM.Identity.Sample.Persistence.Configuration;

// Both passkey mappings live in GM.Identity base configs; these fix them to the sample's aggregates and tables
// (default schema). Null schema keeps them in the model's default schema as before.
public class UserPasskeyConfiguration() : GMUserPasskeyConfiguration<UserPasskey>(null, "user_passkeys");

public class PasskeyChallengeConfiguration() : GMPasskeyChallengeConfiguration<PasskeyChallenge>(null, "passkey_challenges");
