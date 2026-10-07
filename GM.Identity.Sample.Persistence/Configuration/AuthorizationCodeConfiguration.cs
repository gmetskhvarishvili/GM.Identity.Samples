using GM.Identity.Persistence.Configuration;
using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.AuthorizationCodeAggregate;

namespace GM.Identity.Sample.Persistence.Configuration;

// The authorization-code mapping lives in the GM.Identity base config; this fixes it to the sample's aggregate
// and table (default schema).
public class AuthorizationCodeConfiguration() : GMAuthorizationCodeConfiguration<AuthorizationCode>(null, "authorization_codes");
