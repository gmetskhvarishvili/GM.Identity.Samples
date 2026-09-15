using GM.EntityFramework.Domain.Abstractions;
using GM.EntityFramework.Domain.Base;

using System;

namespace GM.Identity.Sample.Domain.BoundedContext.MessagingBoundedContext.EmailTemplateAggregate;

/// <summary>
/// A managed email template addressed by a stable <see cref="Key"/> (e.g. <c>welcome</c>, <c>password-reset</c>).
/// Subject and body support placeholder tokens the sender substitutes at send time. Editable by admins so copy
/// changes need no redeploy.
/// </summary>
public class EmailTemplate : SoftDeletableEntity<Guid>, IAggregateRoot
{
    private EmailTemplate() { } // EF Core materialization

    private EmailTemplate(string key, string subject, string body)
    {
        Id = Guid.NewGuid();
        Key = key;
        Subject = subject;
        Body = body;
    }

    public static EmailTemplate Create(string key, string subject, string body) => new(key, subject, body);

    /// <summary>Stable identifier the application resolves the template by.</summary>
    public string Key { get; private set; } = null!;
    public string Subject { get; private set; } = null!;
    public string Body { get; private set; } = null!;

    public void Update(string subject, string body)
    {
        Subject = subject;
        Body = body;
    }
}
