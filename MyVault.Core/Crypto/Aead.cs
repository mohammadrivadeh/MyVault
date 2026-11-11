using System.Security.Cryptography;
using MyVault.Core.Exceptions;

namespace MyVault.Core.Crypto;

/// <summary>
/// AES-256-GCM authenticated encryption. Every Encrypt call uses a fresh random nonce.
/// Decrypt throws VaultException(WrongPassword) if the tag does not verify — the
/// caller cannot distinguish "wrong password" from "corrupted file", by design.
/// </summary>
public static class Aead
{
    public const int KeySize = 32;
    public const int NonceSize = 12;
    public const int TagSize = 16;

    public static (byte[] Nonce, byte[] Ciphertext, byte[] Tag) Encrypt(
        ReadOnlySpan<byte> key,
        ReadOnlySpan<byte> plaintext,
        ReadOnlySpan<byte> aad)
    {
        if (key.Length != KeySize)
            throw new ArgumentException($"Key must be {KeySize} bytes.", nameof(key));

        byte[] nonce = RandomNumberGenerator.GetBytes(NonceSize);
        byte[] ciphertext = new byte[plaintext.Length];
        byte[] tag = new byte[TagSize];

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plaintext, ciphertext, tag, aad);
        return (nonce, ciphertext, tag);
    }

    public static byte[] Decrypt(
        ReadOnlySpan<byte> key,
        ReadOnlySpan<byte> nonce,
        ReadOnlySpan<byte> ciphertext,
        ReadOnlySpan<byte> tag,
        ReadOnlySpan<byte> aad)
    {
        if (key.Length != KeySize)
            throw new ArgumentException($"Key must be {KeySize} bytes.", nameof(key));
        if (nonce.Length != NonceSize)
            throw new ArgumentException($"Nonce must be {NonceSize} bytes.", nameof(nonce));
        if (tag.Length != TagSize)
            throw new ArgumentException($"Tag must be {TagSize} bytes.", nameof(tag));

        byte[] plaintext = new byte[ciphertext.Length];
        try
        {
            using var aes = new AesGcm(key, TagSize);
            aes.Decrypt(nonce, ciphertext, tag, plaintext, aad);
        }
        catch (CryptographicException ex)
        {
            CryptographicOperations.ZeroMemory(plaintext);
            throw new VaultException(
                VaultError.WrongPassword,
                "Wrong master password or corrupted vault.",
                ex);
        }
        return plaintext;
    }
}