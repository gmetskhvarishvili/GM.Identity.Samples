using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Infrastructure.Services.PasswordSafety;

/// <summary>
/// Checks whether a password appears in a known-breach corpus (e.g. Have I Been Pwned). Lets the app refuse
/// passwords that are compromised even if they satisfy the complexity policy.
/// </summary>
public interface IBreachedPasswordChecker
{
    Task<bool> IsBreachedAsync(string password, CancellationToken cancellationToken);
}

/// <summary>Default no-op checker (breach checking disabled): never reports a password as breached.</summary>
public sealed class NullBreachedPasswordChecker : IBreachedPasswordChecker
{
    public Task<bool> IsBreachedAsync(string password, CancellationToken cancellationToken) => Task.FromResult(false);
}
