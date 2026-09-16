using System;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Infrastructure.Services.Logout;

/// <summary>
/// Reads a previously issued id_token (e.g. an <c>id_token_hint</c> on logout). Verifies it was signed by this
/// OP but tolerates expiry — a logout hint is routinely presented after the id_token has expired.
/// </summary>
public interface IIdTokenReader
{
    /// <summary>
    /// Returns the SSO session id (<c>sid</c>) from a hint whose signature verifies against the OP key, or null
    /// when the token is absent, tampered, or carries no usable <c>sid</c>.
    /// </summary>
    Task<Guid?> TryReadSessionIdAsync(string? idToken, CancellationToken cancellationToken);
}
