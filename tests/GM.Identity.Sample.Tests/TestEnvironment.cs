using GM.Identity.Sample.Infrastructure.Authorization;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace GM.Identity.Sample.Tests;

/// <summary>
/// Process-wide settings for every <c>GmWebApplicationFactory</c> host in this assembly, applied before
/// any test runs.
/// </summary>
internal static class TestEnvironment
{
    /// <summary>
    /// Turns off the scheduled cache reconciliation jobs. Tests seed cache-only roles, users and sessions
    /// straight into Redis. The jobs fire on a :X0/:X5 cron and prune anything with no database row, so a
    /// test that crossed that boundary would lose its grants and fail with 403. Tests that exercise the
    /// jobs turn them back on with <c>WithConfig(CacheReconciliation.EnabledKey, "true")</c>.
    /// </summary>
    [ModuleInitializer]
    [SuppressMessage("Usage", "CA2255:The 'ModuleInitializer' attribute should not be used in libraries",
        Justification = "Test assembly: configures the in-process test hosts before any test runs.")]
    internal static void DisableScheduledCacheReconciliation() =>
        Environment.SetEnvironmentVariable(
            CacheReconciliation.EnabledKey.Replace(":", "__", StringComparison.Ordinal), "false");
}
