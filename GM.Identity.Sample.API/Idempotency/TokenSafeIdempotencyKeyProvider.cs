using System.Linq;
using GM.Idempotency;
using GM.Idempotency.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace GM.Identity.Sample.API.Idempotency;

/// <summary>
/// The <c>Idempotency-Key</c> header provider, minus the OAuth/OIDC endpoints. The idempotency middleware
/// stores a successful response for 24h so a retry can replay it — for <c>/connect/*</c> that response is
/// a fresh access/refresh/id token, and storing it would put live credentials in the cache in clear text
/// (only token hashes are ever persisted). Returning no key there makes those requests ordinary, so a
/// client may send the header on every call without leaking tokens; everywhere else it behaves as usual.
/// </summary>
public sealed class TokenSafeIdempotencyKeyProvider(IOptions<IdempotencyHttpOptions> options)
    : IIdempotencyKeyProvider<HttpContext>
{
    private static readonly PathString[] ExcludedPaths = [new("/connect")];

    private readonly HeaderIdempotencyKeyProvider _inner = new(options);

    public string? GetKey(HttpContext source) =>
        ExcludedPaths.Any(path => source.Request.Path.StartsWithSegments(path))
            ? null
            : _inner.GetKey(source);
}
