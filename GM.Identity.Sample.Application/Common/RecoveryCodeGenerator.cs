using System;
using System.Security.Cryptography;
using System.Text;

namespace GM.Identity.Sample.Application.Common;

/// <summary>
/// Generates human-typable single-use recovery codes. Each code is high-entropy random text formatted as two
/// groups (e.g. <c>7QF2K-9ZD4M</c>); only its hash is ever stored.
/// </summary>
public static class RecoveryCodeGenerator
{
    // Crockford-ish base32 alphabet: no 0/O/1/I/L to avoid transcription errors.
    private const string Alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
    private const int GroupLength = 5;
    private const int Groups = 2;

    /// <summary>Produces a single formatted recovery code.</summary>
    public static string Generate()
    {
        var sb = new StringBuilder(GroupLength * Groups + (Groups - 1));
        for (var group = 0; group < Groups; group++)
        {
            if (group > 0) sb.Append('-');
            for (var i = 0; i < GroupLength; i++)
                sb.Append(Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)]);
        }
        return sb.ToString();
    }

    /// <summary>Produces <paramref name="count"/> recovery codes.</summary>
    public static string[] Generate(int count)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);
        var codes = new string[count];
        for (var i = 0; i < count; i++)
            codes[i] = Generate();
        return codes;
    }
}
