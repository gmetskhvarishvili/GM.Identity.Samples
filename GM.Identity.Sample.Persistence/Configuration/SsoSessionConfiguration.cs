using GM.Identity.Persistence.Configuration;
using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.SsoSessionAggregate;

namespace GM.Identity.Sample.Persistence.Configuration;

// The SSO-session mapping lives in the GM.Identity base config; this fixes it to the sample's aggregate
// (named by IdentityTableNames, without GM.Identity's gm_ prefix).
public class SsoSessionConfiguration : GMSsoSessionConfiguration<SsoSession>
{
    protected override string Schema => IdentityTableNames.Schemas.Authorization;
    protected override string Table => IdentityTableNames.SsoSessions;
}
