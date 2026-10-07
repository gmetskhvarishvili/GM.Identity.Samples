using GM.Identity.Persistence.Configuration;
using GM.Identity.Sample.Domain.BoundedContext.ComplianceBoundedContext.ConsentDocumentAggregate;

namespace GM.Identity.Sample.Persistence.Configuration;

// The consent-document mapping lives in the GM.Identity base config; this fixes it to the sample's
// aggregate and its "compliance" schema table.
public class ConsentDocumentConfiguration() : GMConsentDocumentConfiguration<ConsentDocument>("compliance", "consent_documents");
