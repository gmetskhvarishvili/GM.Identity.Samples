using GM.Identity.Sample.Application.Infrastructure.Services.PasswordSafety;

using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Infrastructure.Services.PasswordSafety;

/// <summary>
/// Have I Been Pwned "Pwned Passwords" checker using the k-anonymity range API: only the first 5 characters of
/// the password's SHA-1 hash are sent; the API returns all suffixes with that prefix and we match locally, so
/// the password itself never leaves the process. Fails open (returns false) on any transport error so a
/// third-party outage never blocks a password change.
/// </summary>
public sealed class HibpBreachedPasswordChecker(System.Net.Http.HttpClient httpClient) : IBreachedPasswordChecker
{
    public async Task<bool> IsBreachedAsync(string password, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(password))
            return false;

        // SHA-1 is required by the HIBP protocol: the Pwned Passwords corpus is indexed by SHA-1, so the
        // k-anonymity prefix must be computed with it. It is not used here as a security primitive.
#pragma warning disable CA5350 // Do Not Use Weak Cryptographic Algorithms
        var sha1 = Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(password)));
#pragma warning restore CA5350
        var prefix = sha1[..5];
        var suffix = sha1[5..];

        try
        {
            using var response = await httpClient.GetAsync($"range/{prefix}", cancellationToken);
            if (!response.IsSuccessStatusCode)
                return false;

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            foreach (var line in body.Split('\n'))
            {
                var separator = line.IndexOf(':');
                if (separator <= 0) continue;
                if (line.AsSpan(0, separator).Trim().Equals(suffix, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
        catch
        {
            return false; // fail open — never block on an HIBP outage
        }
    }
}
