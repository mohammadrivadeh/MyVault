using MyVault.Core.Crypto;
using MyVault.Core.Exceptions;

namespace MyVault.Core.Models;

/// <summary>
/// Plaintext header stored in the vault file. Authenticated as AAD, so any tampering
/// breaks decryption. Contains the KDF parameters + salt needed to re-derive the key.
/// </summary>
public sealed class VaultHeader
{
    public const string CurrentMagic = "MVLT1";
    public const int CurrentFormatVersion = 2;
    public const string Argon2idKdf = "argon2id";

    public string Magic { get; set; } = CurrentMagic;
    public int FormatVersion { get; set; } = CurrentFormatVersion;
    public string Kdf { get; set; } = Argon2idKdf;
    public int MemoryKiB { get; set; } = KeyDerivation.DefaultMemoryKiB;
    public int Iterations { get; set; } = KeyDerivation.DefaultIterations;
    public int Parallelism { get; set; } = KeyDerivation.DefaultParallelism;
    public byte[] Salt { get; set; } = Array.Empty<byte>();

    public static VaultHeader CreateDefault() => new()
    {
        Salt = KeyDerivation.NewSalt()
    };

    public void Validate()
    {
        if (Magic != CurrentMagic)
            throw new VaultException(VaultError.Corrupted, "Not a valid vault file.");

        if (FormatVersion < 1 || FormatVersion > CurrentFormatVersion)
            throw new VaultException(
                VaultError.UnsupportedVersion,
                $"Vault format version {FormatVersion} is not supported by this app version.");

        if (Kdf != Argon2idKdf)
            throw new VaultException(
                VaultError.UnsupportedVersion,
                $"Unsupported KDF '{Kdf}'.");

        if (Salt is not { Length: >= 8 and <= 64 })
            throw new VaultException(VaultError.Corrupted, "Invalid salt in header.");

        if (MemoryKiB < 8 || MemoryKiB > 1024 * 1024 ||
            Iterations < 1 || Iterations > 10 || Parallelism < 1 || Parallelism > 16 ||
            MemoryKiB < 8 * Parallelism)
            throw new VaultException(VaultError.Corrupted, "Invalid KDF parameters in header.");
    }
}