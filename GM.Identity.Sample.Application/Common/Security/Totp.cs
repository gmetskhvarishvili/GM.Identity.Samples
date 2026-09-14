using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace GM.Identity.Sample.Application.Common.Security;

/// <summary>
/// RFC 6238 time-based one-time passwords (TOTP), the scheme authenticator apps (Google Authenticator, Authy,
/// 1Password, …) implement: HMAC-SHA1 over a 30-second counter, truncated to 6 digits. Self-contained so the
/// sample can offer authenticator-app 2FA without any external OTP service.
/// </summary>
public static class Totp
{
    private const int Digits = 6;
    private const int PeriodSeconds = 30;
    private const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    /// <summary>Generates a new random Base32 secret (default 20 bytes, the RFC 4226 recommendation).</summary>
    public static string GenerateSecret(int bytes = 20)
    {
        var buffer = RandomNumberGenerator.GetBytes(bytes);
        return Base32Encode(buffer);
    }

    /// <summary>
    /// Builds the <c>otpauth://totp/...</c> provisioning URI an authenticator app scans (as a QR code) to
    /// enrol the account.
    /// </summary>
    public static string BuildProvisioningUri(string secretBase32, string issuer, string accountName)
    {
        var label = Uri.EscapeDataString($"{issuer}:{accountName}");
        return $"otpauth://totp/{label}?secret={secretBase32}&issuer={Uri.EscapeDataString(issuer)}" +
               $"&algorithm=SHA1&digits={Digits}&period={PeriodSeconds}";
    }

    /// <summary>
    /// Verifies a submitted code against the secret, accepting the current step plus <paramref name="window"/>
    /// steps either side to tolerate clock drift.
    /// </summary>
    public static bool Verify(string secretBase32, string code, int window = 1, DateTimeOffset? now = null)
    {
        if (string.IsNullOrWhiteSpace(secretBase32) || string.IsNullOrWhiteSpace(code))
            return false;

        code = code.Trim();
        byte[] key;
        try { key = Base32Decode(secretBase32); }
        catch { return false; }

        var counter = (now ?? DateTimeOffset.UtcNow).ToUnixTimeSeconds() / PeriodSeconds;
        for (var offset = -window; offset <= window; offset++)
        {
            if (CryptographicOperations.FixedTimeEquals(
                    Encoding.ASCII.GetBytes(Compute(key, counter + offset)),
                    Encoding.ASCII.GetBytes(code)))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>Computes the TOTP code for a specific counter — used by tests to produce a valid code.</summary>
    public static string ComputeForTest(string secretBase32, DateTimeOffset now) =>
        Compute(Base32Decode(secretBase32), now.ToUnixTimeSeconds() / PeriodSeconds);

    private static string Compute(byte[] key, long counter)
    {
        var counterBytes = BitConverter.GetBytes(counter);
        if (BitConverter.IsLittleEndian) Array.Reverse(counterBytes);

        // HMAC-SHA1 is mandated here for interoperability: RFC 6238's default and what every authenticator app
        // (Google Authenticator, Authy, 1Password, …) implements. It is not used as a general-purpose hash.
#pragma warning disable CA5350 // Do Not Use Weak Cryptographic Algorithms
        using var hmac = new HMACSHA1(key);
#pragma warning restore CA5350
        var hash = hmac.ComputeHash(counterBytes);

        var binaryOffset = hash[^1] & 0x0F;
        var binary = ((hash[binaryOffset] & 0x7F) << 24)
                     | ((hash[binaryOffset + 1] & 0xFF) << 16)
                     | ((hash[binaryOffset + 2] & 0xFF) << 8)
                     | (hash[binaryOffset + 3] & 0xFF);

        var otp = binary % (int)Math.Pow(10, Digits);
        return otp.ToString(CultureInfo.InvariantCulture).PadLeft(Digits, '0');
    }

    private static string Base32Encode(byte[] data)
    {
        var sb = new StringBuilder((data.Length + 4) / 5 * 8);
        int buffer = 0, bitsLeft = 0;
        foreach (var b in data)
        {
            buffer = (buffer << 8) | b;
            bitsLeft += 8;
            while (bitsLeft >= 5)
            {
                bitsLeft -= 5;
                sb.Append(Base32Alphabet[(buffer >> bitsLeft) & 0x1F]);
            }
        }
        if (bitsLeft > 0)
            sb.Append(Base32Alphabet[(buffer << (5 - bitsLeft)) & 0x1F]);
        return sb.ToString();
    }

    private static byte[] Base32Decode(string input)
    {
        input = input.TrimEnd('=').ToUpperInvariant();
        var output = new byte[input.Length * 5 / 8];
        int buffer = 0, bitsLeft = 0, index = 0;
        foreach (var c in input)
        {
            var value = Base32Alphabet.IndexOf(c);
            if (value < 0) throw new FormatException("Invalid Base32 character.");
            buffer = (buffer << 5) | value;
            bitsLeft += 5;
            if (bitsLeft >= 8)
            {
                bitsLeft -= 8;
                output[index++] = (byte)((buffer >> bitsLeft) & 0xFF);
            }
        }
        return output;
    }
}
