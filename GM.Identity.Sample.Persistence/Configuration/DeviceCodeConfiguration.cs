using GM.Identity.Persistence.Configuration;
using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.DeviceCodeAggregate;

namespace GM.Identity.Sample.Persistence.Configuration;

// The device-code mapping lives in the GM.Identity base config; this fixes it to the sample's aggregate and
// table (default schema).
public class DeviceCodeConfiguration() : GMDeviceCodeConfiguration<DeviceCode>(null, "device_codes");
