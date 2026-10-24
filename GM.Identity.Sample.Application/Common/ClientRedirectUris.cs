using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.ClientRedirectUriAggregate;
using GM.Identity.Sample.Domain.SeedWork;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Common;

/// <summary>
/// A client's registered redirect URIs — the exact URIs the authorization endpoint may send a code to. Shared by
/// the create and update client commands: the list is trimmed and de-duplicated, and on update the client's set
/// is replaced (URIs no longer listed are soft-removed, new ones added; unchanged ones are left untouched).
/// </summary>
public static class ClientRedirectUris
{
    /// <summary>The URIs to store: trimmed, blanks dropped, duplicates removed (exact match, as authorize compares).</summary>
    public static List<string> Normalize(IEnumerable<string>? uris) =>
        (uris ?? [])
            .Where(u => !string.IsNullOrWhiteSpace(u))
            .Select(u => u.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();

    /// <summary>
    /// True for an absolute, non-file URI — what a redirect or logout endpoint must be. (On Unix a bare path such as
    /// <c>/callback</c> parses as an absolute <c>file://</c> URI, so the file scheme is excluded explicitly.)
    /// </summary>
    public static bool IsAbsoluteUri(string? uri) =>
        Uri.TryCreate(uri, UriKind.Absolute, out var parsed) && parsed.Scheme != Uri.UriSchemeFile;

    /// <summary>Adds redirect URIs for a client being created.</summary>
    public static async Task AddClientRedirectUrisAsync(
        this IUnitOfWork unitOfWork, Guid clientId, IEnumerable<string>? uris, CancellationToken cancellationToken)
    {
        foreach (var uri in Normalize(uris))
            await unitOfWork.ClientRedirectUriRepository.AddAsync(ClientRedirectUri.Create(clientId, uri), cancellationToken);
    }

    /// <summary>Makes the client's live redirect URIs exactly <paramref name="uris"/> (an empty list clears them).</summary>
    public static async Task ReplaceClientRedirectUrisAsync(
        this IUnitOfWork unitOfWork, Guid clientId, IEnumerable<string> uris, CancellationToken cancellationToken)
    {
        var wanted = Normalize(uris);
        var current = await unitOfWork.ClientRedirectUriRepository
            .Query(false, null)
            .Where(x => x.ClientId == clientId && !x.IsDeleted)
            .ToListAsync(cancellationToken);

        foreach (var stale in current.Where(x => !wanted.Contains(x.Uri, StringComparer.Ordinal)))
        {
            stale.SoftRemove();
            unitOfWork.ClientRedirectUriRepository.Update(stale);
        }

        foreach (var uri in wanted.Where(u => current.All(x => !string.Equals(x.Uri, u, StringComparison.Ordinal))))
            await unitOfWork.ClientRedirectUriRepository.AddAsync(ClientRedirectUri.Create(clientId, uri), cancellationToken);
    }
}
