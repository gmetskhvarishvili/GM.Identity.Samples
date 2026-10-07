using GM.Identity.Persistence.Configuration;
using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.UserClientConsentAggregate;

namespace GM.Identity.Sample.Persistence.Configuration;

// The per-client consent mapping lives in the GM.Identity base config; this fixes it to the sample's aggregate
// and table (default schema).
public class UserClientConsentConfiguration() : GMUserClientConsentConfiguration<UserClientConsent>(null, "user_client_consents");
