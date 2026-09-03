namespace GM.Identity.Sample.Application.Common;

/// <summary>
/// Base for read DTOs returned by list and details queries. Exposes the audit timestamps as
/// preformatted strings (see <see cref="DateTimeFormat"/>); the DateTime-to-string conversion is
/// configured once in <see cref="MappingRegister"/>.
/// </summary>
public abstract class AuditableDto
{
    public const string DateTimeFormat = "dd/MM/yyyy HH:mm:ss";

    public string CreatedAt { get; set; } = string.Empty;
    public string? UpdatedAt { get; set; }
}
