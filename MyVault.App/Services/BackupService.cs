using MyVault.Core.Storage;
using System.IO;

namespace MyVault.App.Services;

public sealed class BackupService
{
    private readonly VaultRepository _repository;

    public BackupService(VaultRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    /// <summary>
    /// Copies the current vault file to <paramref name="destinationPath"/>.
    /// The copy is byte-identical to the live vault (already encrypted).
    /// Verifies the copy decrypts before returning success.
    /// </summary>
    public void SaveEncryptedCopy(string destinationPath)
    {
        ArgumentException.ThrowIfNullOrEmpty(destinationPath);

        if (!_repository.IsUnlocked)
            throw new InvalidOperationException("Vault is locked.");

        if (string.Equals(Path.GetFullPath(destinationPath), _repository.VaultPath, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Choose a destination different from the active vault.");
        byte[] blob = VaultFile.Read(_repository.VaultPath);

        if (!_repository.VerifyExternalVault(blob))
        {
            Array.Clear(blob);
            throw new InvalidOperationException(
                "The current vault file could not be verified — refusing to save an invalid backup.");
        }

        try
        {
            string? dir = Path.GetDirectoryName(Path.GetFullPath(destinationPath));
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            VaultFile.WriteAtomic(destinationPath, blob, maxBackups: 0);
            byte[] copy = VaultFile.Read(destinationPath);
            try { if (!copy.AsSpan().SequenceEqual(blob) || !_repository.VerifyExternalVault(copy)) throw new IOException("Backup verification failed."); }
            finally { Array.Clear(copy); }
        }
        finally
        {
            Array.Clear(blob);
        }
    }
}