using GM.Identity.Persistence.Configuration;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.ClientRedirectUriAggregate;

namespace GM.Identity.Sample.Persistence.Configuration;

// The client-redirect-URI mapping lives in the GM.Identity base config; this fixes it to the sample's aggregate
// (named by IdentityTableNames, without GM.Identity's gm_ prefix).
public class ClientRedirectUriConfiguration : GMClientRedirectUriConfiguration<ClientRedirectUri>
{
    protected override string Schema => IdentityTableNames.Schemas.Identity;
    protected override string Table => IdentityTableNames.ClientRedirectUris;
}
