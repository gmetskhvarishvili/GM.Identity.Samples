namespace GM.Identity.Sample.API.Common;

/// <summary>
/// Base for API read models returned by list and details endpoints. Carries the audit timestamps
/// as preformatted strings (dd/MM/yyyy HH:mm:ss), copied straight from the application DTOs which
/// already format them.
/// </summary>
public abstract class AuditableModel
{
    public string CreatedAt { get; set; } = string.Empty;
    public string? UpdatedAt { get; set; }
}
