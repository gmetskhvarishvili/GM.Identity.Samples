using GM.Scheduling;
using Microsoft.Extensions.Configuration;

namespace GM.Identity.Sample.Infrastructure.Authorization;

/// <summary>
/// The switch shared by the cache reconciliation jobs. Reconciliation is on by default. Setting
/// <c>Authorization:CacheReconciliation:Enabled</c> to <c>false</c> makes each scheduled fire a no-op.
/// The test suite does this: tests seed cache-only roles, users and sessions, and a reconcile that fires
/// mid-test prunes them as orphans.
/// </summary>
public static class CacheReconciliation
{
    public const string EnabledKey = "Authorization:CacheReconciliation:Enabled";

    public static bool IsEnabled(IConfiguration configuration) =>
        configuration.GetValue(EnabledKey, defaultValue: true);

    public static JobExecutionResult Disabled() =>
        JobExecutionResult.Success(0, $"Skipped: cache reconciliation is disabled ({EnabledKey}=false).");
}
