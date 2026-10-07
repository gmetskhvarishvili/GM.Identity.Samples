using GM.Identity.Persistence.Configuration;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.ClientRedirectUriAggregate;

namespace GM.Identity.Sample.Persistence.Configuration;

// The client-redirect-URI mapping lives in the GM.Identity base config; this fixes it to the sample's aggregate
// and table (default schema).
public class ClientRedirectUriConfiguration() : GMClientRedirectUriConfiguration<ClientRedirectUri>(null, "client_redirect_uris");
