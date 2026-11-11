using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;

namespace MyVault.Core.Crypto;

public static class KeyDerivation
{
    public const int SaltSize = 32;
    public const int KeySize = 32;

    // Argon2id defaults agreed for this app: 256 MiB, 3 iterations, 4 lanes.
    public const int DefaultMemoryKiB = 256 * 1024;  // 256 MiB
    public const int DefaultIterations = 3;
    public const int DefaultParallelism = 4;

    public static byte[] NewSalt() => RandomNumberGenerator.GetBytes(SaltSize);

    public static byte[] DeriveKey(
        string masterPassword,
        ReadOnlySpan<byte> salt,
        int memoryKiB = DefaultMemoryKiB,
        int iterations = DefaultIterations,
        int parallelism = DefaultParallelism)
    {
        if (string.IsNullOrEmpty(masterPassword))
            throw new ArgumentException("Master password cannot be empty.", nameof(masterPassword));
        if (salt.Length < 8 || salt.Length > 64)
            throw new ArgumentException("Salt must be at least 8 bytes.", nameof(salt));
        if (memoryKiB < 8 || memoryKiB > 1024 * 1024 || memoryKiB < 8 * parallelism)
            throw new ArgumentOutOfRangeException(nameof(memoryKiB));
        if (iterations < 1 || iterations > 10)
            throw new ArgumentOutOfRangeException(nameof(iterations));
        if (parallelism < 1 || parallelism > 16)
            throw new ArgumentOutOfRangeException(nameof(parallelism));

        byte[] passwordBytes = Encoding.UTF8.GetBytes(masterPassword);
        try
        {
            using var argon = new Argon2id(passwordBytes)
            {
                Salt = salt.ToArray(),
                MemorySize = memoryKiB,
                Iterations = iterations,
                DegreeOfParallelism = parallelism
            };
            return argon.GetBytes(KeySize);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(passwordBytes);
        }
    }
}