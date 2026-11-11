using System.Security.Cryptography;
using MyVault.Core.Crypto;
using MyVault.Core.Exceptions;
using MyVault.Core.Models;

namespace MyVault.Core.Storage;

/// <summary>
/// High-level vault facade used by the UI.
/// Owns the SecureBuffer holding the derived AES key and the in-memory VaultDocument.
/// </summary>
public sealed class VaultRepository : IDisposable
{
    private SecureBuffer? _key;
    private bool _disposed;
    private readonly object _gate = new();
    private FileStream? _lease;

    public string VaultPath { get; }
    public int MaxBackups { get; set; } = 5;

    public VaultDocument Document { get; private set; }
    public VaultHeader Header { get; private set; }

    public bool IsUnlocked => _key is not null;
    public bool IsDirty { get; private set; }

    private VaultRepository(string path, VaultDocument document, VaultHeader header, SecureBuffer key, FileStream lease)
    {
        VaultPath = path; Document = document; Header = header; _key = key; _lease = lease;
    }

    private static FileStream AcquireLease(string path)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            return new FileStream(path + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        { throw new VaultException(VaultError.Io, "Vault is already open or cannot be locked.", ex); }
    }

    public static VaultRepository CreateNew(string path, string masterPassword)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        ArgumentException.ThrowIfNullOrEmpty(masterPassword);
        string fullPath = Path.GetFullPath(path);
        var lease = AcquireLease(fullPath);
        try
        {
            if (File.Exists(fullPath)) throw new VaultException(VaultError.InvalidParameters, "A vault file already exists at that path.");
            var header = VaultHeader.CreateDefault();
            byte[] key = KeyDerivation.DeriveKey(masterPassword, header.Salt);
            try
            {
                var repo = new VaultRepository(fullPath, new VaultDocument(), header, new SecureBuffer(key), lease);
                try { repo.Save(); return repo; } catch { repo.Dispose(); throw; }
            }
            finally { CryptographicOperations.ZeroMemory(key); }
        }
        catch { lease.Dispose(); throw; }
    }

    public static VaultRepository Open(string path, string masterPassword)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        ArgumentException.ThrowIfNullOrEmpty(masterPassword);
        string fullPath = Path.GetFullPath(path);
        var lease = AcquireLease(fullPath);
        try
        {
            byte[] blob = VaultFile.Read(fullPath);
            try
            {
                var header = VaultFile.ReadHeader(blob);
                byte[] key = KeyDerivation.DeriveKey(masterPassword, header.Salt, header.MemoryKiB, header.Iterations, header.Parallelism);
                try { return new VaultRepository(fullPath, VaultFile.Decrypt(blob, key), header, new SecureBuffer(key), lease); }
                finally { CryptographicOperations.ZeroMemory(key); }
            }
            finally { CryptographicOperations.ZeroMemory(blob); }
        }
        catch { lease.Dispose(); throw; }
    }

    // ---------------------------------------------------------------- save

    public void Save()
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            if (_key is null)
                throw new InvalidOperationException("Vault is locked.");

            Document.LastSavedAt = DateTimeOffset.UtcNow;
            byte[] blob = VaultFile.Encrypt(Document, _key.Span, Header);
            try { VaultFile.WriteAtomic(VaultPath, blob, MaxBackups); }
            finally { CryptographicOperations.ZeroMemory(blob); }
            IsDirty = false;

        }
    }

    public void MarkDirty() => IsDirty = true;

    // ---------------------------------------------------------------- change password

    public void ChangeMasterPassword(string newMasterPassword)
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            if (_key is null)
                throw new InvalidOperationException("Vault is locked.");
            ArgumentException.ThrowIfNullOrEmpty(newMasterPassword);

            var newHeader = VaultHeader.CreateDefault();
            newHeader.Salt = KeyDerivation.NewSalt();

            byte[] newKey = KeyDerivation.DeriveKey(newMasterPassword, newHeader.Salt);
            try
            {
                byte[] blob = VaultFile.Encrypt(Document, newKey, newHeader);

                try { VaultFile.WriteAtomic(VaultPath, blob, MaxBackups); }
                finally { CryptographicOperations.ZeroMemory(blob); }

                // Only swap state after the write succeeded.
                SecureBuffer oldKey = _key;
                _key = new SecureBuffer(newKey);
                Header = newHeader;
                IsDirty = false;

                oldKey.Dispose();
            }
            finally
            {
                CryptographicOperations.ZeroMemory(newKey);
            }

        }
    }
    /// <summary>
    /// Constant-time check of the current master password against the in-memory key.
    /// Used by the "Change master password" dialog to verify the user before rekeying.
    /// </summary>
    public bool VerifyPassword(string password)
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            if (_key is null)
                throw new InvalidOperationException("Vault is locked.");
            ArgumentException.ThrowIfNullOrEmpty(password);

            byte[] candidate = KeyDerivation.DeriveKey(
                password,
                Header.Salt,
                Header.MemoryKiB,
                Header.Iterations,
                Header.Parallelism);

            try
            {
                return CryptographicOperations.FixedTimeEquals(candidate, _key.Span);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(candidate);
            }

        }
    }

    /// <summary>
    /// Decrypts an arbitrary vault blob with the current in-memory key.
    /// Used to verify that a just-copied backup file actually opens.
    /// </summary>
    public bool VerifyExternalVault(byte[] blob)
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            if (_key is null)
                throw new InvalidOperationException("Vault is locked.");
            ArgumentNullException.ThrowIfNull(blob);

            try
            {
                VaultFile.Decrypt(blob, _key.Span);
                return true;
            }
            catch
            {
                return false;
            }

        }
    }

    // ---------------------------------------------------------------- lock / dispose

    public void Lock()
    {
        lock (_gate)
        {
            if (_disposed) return;

            _key?.Dispose();
            _key = null;

            // Wipe references to entries so they can be GC'd promptly.
            if (Document is not null)
            {
                foreach (var entry in Document.Entries)
                {
                    entry.Password = string.Empty; entry.Username = string.Empty; entry.Notes = string.Empty; entry.TotpSecret = null;
                }
                Document.Entries.Clear();
                Document.Categories.Clear();
            }
            Document = new VaultDocument();
            IsDirty = false;
            _lease?.Dispose();
            _lease = null;

        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed) return;
            Lock();
            _disposed = true;

        }
    }


    /// <summary>Apply an edit and persist it, restoring memory if persistence fails.</summary>
    public void Update(Action<VaultDocument> edit)
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            if (_key is null) throw new InvalidOperationException("Vault is locked.");
            byte[] before = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(Document);
            try { edit(Document); Save(); }
            catch { Document = System.Text.Json.JsonSerializer.Deserialize<VaultDocument>(before)!; throw; }
            finally { CryptographicOperations.ZeroMemory(before); }
        }
    }

    public void RestoreBackup(byte[] blob)
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            if (_key is null) throw new InvalidOperationException("Vault is locked.");
            VaultFile.ReadHeader(blob);
            var restored = VaultFile.Decrypt(blob, _key.Span);
            Update(d => { d.Entries = restored.Entries; d.Categories = restored.Categories; d.CreatedAt = restored.CreatedAt; });
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(VaultRepository));
    }
}