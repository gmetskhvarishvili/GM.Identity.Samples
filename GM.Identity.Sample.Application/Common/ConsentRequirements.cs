using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.ConsentDocumentAggregate;
using GM.Identity.Sample.Domain.SeedWork;
using Microsoft.EntityFrameworkCore;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Common;

/// <summary>
/// Computes which mandatory consent documents a user still owes — the documents whose <em>current</em> version the
/// user has not yet accepted. Shared by the login gate (see AuthorizeCommand) and the pending-consents query so
/// both apply exactly the same rule.
/// </summary>
public static class ConsentRequirements
{
    /// <summary>
    /// Returns the mandatory <see cref="ConsentDocument"/>s the user has not accepted at their current version
    /// (never accepted, or accepted only an older version). Empty when the user is fully up to date.
    /// </summary>
    public static async Task<IReadOnlyList<ConsentDocument>> GetPendingAsync(
        this IUnitOfWork unitOfWork, Guid userId, CancellationToken cancellationToken)
    {
        var mandatory = await unitOfWork.ConsentDocumentRepository
            .Query(false, null)
            .Where(d => d.IsMandatory && d.IsActive && !d.IsDeleted && !d.IsHidden)
            .ToListAsync(cancellationToken);

        if (mandatory.Count == 0)
            return Array.Empty<ConsentDocument>();

        // Authentication is cross-tenant, and consent documents are global; UserConsent is not tenant-owned, so
        // ignore query filters and re-apply the visibility flags explicitly.
        var acceptedVersionsByType = (await unitOfWork.UserConsentRepository
                .Query(false, null)
                .IgnoreQueryFilters()
                .Where(c => c.UserId == userId && c.IsActive && !c.IsDeleted && !c.IsHidden)
                .Select(c => new { c.ConsentType, c.DocumentVersion })
                .ToListAsync(cancellationToken))
            .GroupBy(c => c.ConsentType, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Select(x => x.DocumentVersion).ToHashSet(StringComparer.Ordinal),
                StringComparer.Ordinal);

        return mandatory
            .Where(d => !acceptedVersionsByType.TryGetValue(d.ConsentType, out var versions)
                        || !versions.Contains(d.CurrentVersion))
            .ToList();
    }
}
