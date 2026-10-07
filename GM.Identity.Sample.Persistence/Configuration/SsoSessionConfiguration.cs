using GM.Identity.Persistence.Configuration;
using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.SsoSessionAggregate;

namespace GM.Identity.Sample.Persistence.Configuration;

// The SSO-session mapping lives in the GM.Identity base config; this fixes it to the sample's aggregate and
// table (default schema).
public class SsoSessionConfiguration() : GMSsoSessionConfiguration<SsoSession>(null, "sso_sessions");
