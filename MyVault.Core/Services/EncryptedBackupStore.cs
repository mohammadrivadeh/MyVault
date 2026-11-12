using System.Security.Cryptography;
using System.Text;
using MyVault.Core.Storage;

namespace MyVault.Core.Services;

public static class EncryptedBackupStore
{
    public static string Write(byte[] encrypted, string directory, string vaultPath, int keep = 10)
    {
        if (keep is < 1 or > 50) throw new ArgumentOutOfRangeException(nameof(keep));
        VaultFile.ReadHeader(encrypted);
        string prefix = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Path.GetFullPath(vaultPath).ToUpperInvariant())))[..16];
        Directory.CreateDirectory(directory);
        string name = $"{prefix}-{DateTimeOffset.UtcNow:yyyyMMddHHmmssfffffff}-{Guid.NewGuid():N}.vault";
        string destination = Path.Combine(directory, name);
        VaultFile.WriteAtomic(destination, encrypted, maxBackups: 0);
        byte[] copy = VaultFile.Read(destination);
        try { if (!copy.AsSpan().SequenceEqual(encrypted)) throw new IOException("Backup verification failed."); }
        finally { CryptographicOperations.ZeroMemory(copy); }
        // Only rotate our own strict names. Never delete arbitrary user files from the chosen directory.
        var own = Directory.EnumerateFiles(directory, prefix + "-*.vault")
            .Where(p => System.Text.RegularExpressions.Regex.IsMatch(Path.GetFileName(p), "^" + prefix + "-[0-9]{21}-[0-9a-f]{32}\\.vault$"))
            .OrderByDescending(Path.GetFileName, StringComparer.Ordinal).ToList();
        foreach (string file in own.Skip(keep))
            if (!string.Equals(Path.GetFullPath(file), Path.GetFullPath(vaultPath), StringComparison.OrdinalIgnoreCase)) File.Delete(file);
        return destination;
    }
    public static bool IsDue(DateTimeOffset? last, DateTimeOffset now, int minutes) =>
        minutes > 0 && (last is null || now - last.Value >= TimeSpan.FromMinutes(minutes));
}
