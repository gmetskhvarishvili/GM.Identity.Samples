namespace GM.Identity.Sample.Application.Common;

/// <summary>
/// Data-retention policy, bound from the <c>Retention</c> configuration section. Governs how long the durable
/// audit (domain-event) log is kept before old entries are reaped.
/// </summary>
public sealed class RetentionOptions
{
    public const string SectionName = "Retention";

    /// <summary>How many days of audit-log history to retain. Default: one year.</summary>
    public int AuditRetentionDays { get; set; } = 365;
}
