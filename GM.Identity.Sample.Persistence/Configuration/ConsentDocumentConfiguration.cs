using GM.Identity.Persistence.Configuration;
using GM.Identity.Sample.Domain.BoundedContext.ComplianceBoundedContext.ConsentDocumentAggregate;

namespace GM.Identity.Sample.Persistence.Configuration;

// The consent-document mapping lives in the GM.Identity base config; this fixes it to the sample's
// aggregate (named by IdentityTableNames, without GM.Identity's gm_ prefix).
public class ConsentDocumentConfiguration : GMConsentDocumentConfiguration<ConsentDocument>
{
    protected override string Schema => IdentityTableNames.Schemas.Compliance;
    protected override string Table => IdentityTableNames.ConsentDocuments;
}
