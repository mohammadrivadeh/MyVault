using System.Buffers.Binary;
using System.Security.Cryptography;

namespace MyVault.Core.Services;

/// <summary>RFC 6238 SHA-1, six digits, 30-second steps. No network access.</summary>
public static class Totp
{
    public static string Generate(string secret, DateTimeOffset now)
    {
        byte[] key = Decode(secret);
        try
        {
            Span<byte> counter = stackalloc byte[8];
            BinaryPrimitives.WriteInt64BigEndian(counter, now.ToUnixTimeSeconds() / 30);
            byte[] hash = HMACSHA1.HashData(key, counter);
            int offset = hash[^1] & 15;
            int value = BinaryPrimitives.ReadInt32BigEndian(hash.AsSpan(offset, 4)) & int.MaxValue;
            return (value % 1000000).ToString("D6", System.Globalization.CultureInfo.InvariantCulture);
        }
        finally { CryptographicOperations.ZeroMemory(key); }
    }

    public static bool IsValid(string? secret)
    {
        try { var key = Decode(secret ?? ""); CryptographicOperations.ZeroMemory(key); return true; }
        catch (ArgumentException) { return false; }
    }

    private static byte[] Decode(string secret)
    {
        string value = secret.Replace(" ", "").TrimEnd('=').ToUpperInvariant();
        if (value.Length < 16 || value.Length > 256) throw new ArgumentException("Use a Base32 TOTP secret (16–256 characters).");
        var bytes = new List<byte>();
        int buffer = 0, bits = 0;
        foreach (char c in value)
        {
            int n = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567".IndexOf(c);
            if (n < 0) throw new ArgumentException("Invalid Base32 secret.");
            buffer = (buffer << 5) | n; bits += 5;
            if (bits >= 8) { bits -= 8; bytes.Add((byte)(buffer >> bits)); }
        }
        return bytes.ToArray();
    }
}
