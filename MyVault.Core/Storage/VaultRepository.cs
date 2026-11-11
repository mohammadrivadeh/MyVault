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

    public string VaultPath { get; }
    public int MaxBackups { get; set; } = 5;

    public VaultDocument Document { get; private set; }
    public VaultHeader Header { get; private set; }

    public bool IsUnlocked => _key is not null;
    public bool IsDirty { get; private set; }

    private VaultRepository(string path, VaultDocument document, VaultHeader header, SecureBuffer key)
    {
        VaultPath = path;
        Document = document;
        Header = header;
        _key = key;
    }

    // ---------------------------------------------------------------- create

    public static VaultRepository CreateNew(string path, string masterPassword)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        ArgumentException.ThrowIfNullOrEmpty(masterPassword);

        string fullPath = Path.GetFullPath(path);
        if (File.Exists(fullPath))
            throw new VaultException(VaultError.InvalidParameters,
                "A vault file already exists at that path.");

        var header = VaultHeader.CreateDefault();
        var key = KeyDerivation.DeriveKey(masterPassword, header.Salt);
        try
        {
            var repo = new VaultRepository(fullPath, new VaultDocument(), header, new SecureBuffer(key));
            repo.Save();
            return repo;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }
    }

    // ---------------------------------------------------------------- open

    public static VaultRepository Open(string path, string masterPassword)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        ArgumentException.ThrowIfNullOrEmpty(masterPassword);

        string fullPath = Path.GetFullPath(path);
        byte[] blob = VaultFile.Read(fullPath);
        VaultHeader header = VaultFile.ReadHeader(blob);

        byte[] key = KeyDerivation.DeriveKey(
            masterPassword,
            header.Salt,
            header.MemoryKiB,
            header.Iterations,
            header.Parallelism);

        try
        {
            VaultDocument document = VaultFile.Decrypt(blob, key);
            return new VaultRepository(fullPath, document, header, new SecureBuffer(key));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
            CryptographicOperations.ZeroMemory(blob);
        }
    }

    // ---------------------------------------------------------------- save

    public void Save()
    {
        ThrowIfDisposed();
        if (_key is null)
            throw new InvalidOperationException("Vault is locked.");

        Document.LastSavedAt = DateTimeOffset.UtcNow;
        byte[] blob = VaultFile.Encrypt(Document, _key.Span, Header);
        VaultFile.WriteAtomic(VaultPath, blob, MaxBackups);
        CryptographicOperations.ZeroMemory(blob);
        IsDirty = false;
    }

    public void MarkDirty() => IsDirty = true;

    // ---------------------------------------------------------------- change password

    public void ChangeMasterPassword(string newMasterPassword)
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
            SecureBuffer newKeyBuffer = new(newKey);
            byte[] blob = VaultFile.Encrypt(Document, newKey, newHeader);

            VaultFile.WriteAtomic(VaultPath, blob, MaxBackups);
            CryptographicOperations.ZeroMemory(blob);

            // Only swap state after the write succeeded.
            SecureBuffer oldKey = _key;
            _key = newKeyBuffer;
            Header = newHeader;
            IsDirty = false;

            oldKey.Dispose();
        }
        finally
        {
            CryptographicOperations.ZeroMemory(newKey);
        }
    }
    /// <summary>
    /// Constant-time check of the current master password against the in-memory key.
    /// Used by the "Change master password" dialog to verify the user before rekeying.
    /// </summary>
    public bool VerifyPassword(string password)
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

    /// <summary>
    /// Decrypts an arbitrary vault blob with the current in-memory key.
    /// Used to verify that a just-copied backup file actually opens.
    /// </summary>
    public bool VerifyExternalVault(byte[] blob)
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

    // ---------------------------------------------------------------- lock / dispose

    public void Lock()
    {
        if (_disposed) return;

        _key?.Dispose();
        _key = null;

        // Wipe references to entries so they can be GC'd promptly.
        if (Document is not null)
        {
            Document.Entries.Clear();
        }
        Document = new VaultDocument();
        IsDirty = false;
    }

    public void Dispose()
    {
        if (_disposed) return;
        Lock();
        _disposed = true;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(VaultRepository));
    }
}