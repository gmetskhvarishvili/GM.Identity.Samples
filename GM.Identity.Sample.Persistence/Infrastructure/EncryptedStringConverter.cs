using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

using System;
using System.Security.Cryptography;

namespace GM.Identity.Sample.Persistence.Infrastructure;

/// <summary>
/// An EF value converter that encrypts a string column at rest with AES-256-CBC (random IV per value, prepended
/// to the ciphertext, base64-encoded). Use it for PII that is stored but never queried by value. Because the IV
/// is random, the same plaintext yields different ciphertext each time (no equality search on the column).
/// </summary>
public sealed class EncryptedStringConverter : ValueConverter<string, string>
{
    public EncryptedStringConverter(byte[] key)
        : base(v => Encrypt(v, key), v => Decrypt(v, key))
    {
    }

    private static string Encrypt(string plaintext, byte[] key)
    {
        if (string.IsNullOrEmpty(plaintext)) return plaintext;

        using var aes = Aes.Create();
        aes.Key = key;
        aes.GenerateIV();
        var cipher = aes.EncryptCbc(System.Text.Encoding.UTF8.GetBytes(plaintext), aes.IV);

        var combined = new byte[aes.IV.Length + cipher.Length];
        Buffer.BlockCopy(aes.IV, 0, combined, 0, aes.IV.Length);
        Buffer.BlockCopy(cipher, 0, combined, aes.IV.Length, cipher.Length);
        return Convert.ToBase64String(combined);
    }

    private static string Decrypt(string stored, byte[] key)
    {
        if (string.IsNullOrEmpty(stored)) return stored;

        var combined = Convert.FromBase64String(stored);
        using var aes = Aes.Create();
        aes.Key = key;
        var iv = new byte[16];
        Buffer.BlockCopy(combined, 0, iv, 0, iv.Length);
        var cipher = new byte[combined.Length - iv.Length];
        Buffer.BlockCopy(combined, iv.Length, cipher, 0, cipher.Length);
        return System.Text.Encoding.UTF8.GetString(aes.DecryptCbc(cipher, iv));
    }
}
