using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MyVault.Core.Crypto;
using MyVault.Core.Exceptions;
using MyVault.Core.Models;

namespace MyVault.Core.Storage;

/// <summary>
/// Byte-level vault file format.
///
///   [ Magic (5) ][ HeaderLen (4, BE) ][ HeaderJson (N, AAD) ][ Nonce (12) ][ Ciphertext (M) ][ Tag (16) ]
///
/// The header (magic + length + JSON) is authenticated as AAD. Any tampering breaks decryption.
/// One fresh 12-byte nonce is generated every Encrypt call; the salt inside the header is
/// generated once per vault (and again when the master password changes).
/// </summary>
public static class VaultFile
{
    public const long MaxFileBytes = 64 * 1024 * 1024;
    public const int MagicSize = 5;
    public const int HeaderLenSize = 4;
    public static readonly string Magic = VaultHeader.CurrentMagic;

    private const int MaxHeaderBytes = 64 * 1024; // sanity cap; a real header is ~150 bytes

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        // Keep every field even if default, so the format is future-proof.
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
    };

    // ---------------------------------------------------------------- header

    public static VaultHeader ReadHeader(ReadOnlySpan<byte> blob)
    {
        if (blob.Length > MaxFileBytes) throw new VaultException(VaultError.Corrupted, "Vault exceeds size limit.");
        int aadLength = GetAadLength(blob);
        var headerJson = blob.Slice(MagicSize + HeaderLenSize, aadLength - MagicSize - HeaderLenSize);

        VaultHeader? header;
        try
        {
            header = JsonSerializer.Deserialize<VaultHeader>(headerJson, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new VaultException(VaultError.Corrupted, "Corrupted header JSON.", ex);
        }

        if (header is null)
            throw new VaultException(VaultError.Corrupted, "Empty header.");

        header.Validate();
        return header;
    }

    // ---------------------------------------------------------------- encrypt

    public static byte[] Encrypt(VaultDocument document, ReadOnlySpan<byte> key, VaultHeader header)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(header);
        header.Validate();
        document.Validate();

        var headerJson = JsonSerializer.SerializeToUtf8Bytes(header, JsonOptions);
        int headerLen = headerJson.Length;
        if (headerLen <= 0 || headerLen > MaxHeaderBytes)
            throw new VaultException(VaultError.Corrupted, "Header too large.");

        // AAD = magic + headerLen + headerJson
        int aadLength = MagicSize + HeaderLenSize + headerLen;
        byte[] aad = new byte[aadLength];
        Encoding.ASCII.GetBytes(Magic, aad.AsSpan(0, MagicSize));
        BinaryPrimitives.WriteInt32BigEndian(aad.AsSpan(MagicSize, HeaderLenSize), headerLen);
        headerJson.CopyTo(aad.AsSpan(MagicSize + HeaderLenSize, headerLen));

        byte[] docJson = JsonSerializer.SerializeToUtf8Bytes(document, JsonOptions);
        try
        {
            var (nonce, ciphertext, tag) = Aead.Encrypt(key, docJson, aad);

            int total = aadLength + nonce.Length + ciphertext.Length + tag.Length;
            byte[] result = new byte[total];

            int offset = 0;
            aad.CopyTo(result.AsSpan(offset)); offset += aadLength;
            nonce.CopyTo(result.AsSpan(offset)); offset += nonce.Length;
            ciphertext.CopyTo(result.AsSpan(offset)); offset += ciphertext.Length;
            tag.CopyTo(result.AsSpan(offset));

            return result;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(docJson);
        }
    }

    // ---------------------------------------------------------------- decrypt

    public static VaultDocument Decrypt(ReadOnlySpan<byte> blob, ReadOnlySpan<byte> key)
    {
        if (blob.Length > MaxFileBytes) throw new VaultException(VaultError.Corrupted, "Vault exceeds size limit.");
        int aadLength = GetAadLength(blob);

        int minimum = aadLength + Aead.NonceSize + Aead.TagSize;
        if (blob.Length < minimum)
            throw new VaultException(VaultError.Corrupted, "File truncated.");

        var aad = blob.Slice(0, aadLength);
        var nonce = blob.Slice(aadLength, Aead.NonceSize);
        var tag = blob.Slice(blob.Length - Aead.TagSize, Aead.TagSize);
        int ctLength = blob.Length - aadLength - Aead.NonceSize - Aead.TagSize;
        var ciphertext = blob.Slice(aadLength + Aead.NonceSize, ctLength);

        byte[] docJson = Aead.Decrypt(key, nonce, ciphertext, tag, aad);
        try
        {
            VaultDocument? doc;
            try
            {
                doc = JsonSerializer.Deserialize<VaultDocument>(docJson, JsonOptions);
            }
            catch (JsonException ex)
            {
                throw new VaultException(VaultError.Corrupted, "Corrupted document payload.", ex);
            }
            if (doc is null)
                throw new VaultException(VaultError.Corrupted, "Empty document.");
            doc.Validate();
            return doc;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(docJson);
        }
    }

    // ---------------------------------------------------------------- file I/O

    public static byte[] Read(string path)
    {
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (stream.Length > MaxFileBytes)
                throw new VaultException(VaultError.Corrupted, "Vault exceeds the 64 MiB limit.");
            byte[] data = new byte[checked((int)stream.Length)];
            stream.ReadExactly(data);
            return data;
        }
        catch (FileNotFoundException ex)
        {
            throw new VaultException(VaultError.Io, $"Vault not found: {path}", ex);
        }
        catch (IOException ex)
        {
            throw new VaultException(VaultError.Io, "Could not read vault file.", ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new VaultException(VaultError.Io, "Access denied reading vault file.", ex);
        }
    }

    /// <summary>
    /// Writes <paramref name="blob"/> atomically to <paramref name="path"/> and rotates
    /// up to <paramref name="maxBackups"/> previous versions as "path.bak.1".."path.bak.N"
    /// (`.bak.1` is the most recent backup).
    /// </summary>
    public static void WriteAtomic(string path, byte[] blob, int maxBackups = 5)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        ArgumentNullException.ThrowIfNull(blob);
        if (maxBackups < 0) throw new ArgumentOutOfRangeException(nameof(maxBackups));

        string fullPath = Path.GetFullPath(path);
        string? directory = Path.GetDirectoryName(fullPath);
        if (string.IsNullOrEmpty(directory))
            throw new VaultException(VaultError.Io, "Path has no directory component.");
        Directory.CreateDirectory(directory);

        if (maxBackups > 0 && File.Exists(fullPath))
            RotateBackups(fullPath, maxBackups);

        if (blob.LongLength > MaxFileBytes) throw new VaultException(VaultError.InvalidParameters, "Vault exceeds size limit.");
        string tmpPath = fullPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var fs = new FileStream(
                tmpPath, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                bufferSize: 4096, FileOptions.WriteThrough))
            {
                fs.Write(blob, 0, blob.Length);
                fs.Flush(flushToDisk: true);
            }
            File.Move(tmpPath, fullPath, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            TryDelete(tmpPath);
            throw new VaultException(VaultError.Io, "Could not write vault file.", ex);
        }
    }

    // ---------------------------------------------------------------- internals

    private static int GetAadLength(ReadOnlySpan<byte> blob)
    {
        if (blob.Length < MagicSize + HeaderLenSize)
            throw new VaultException(VaultError.Corrupted, "File too small to be a vault.");

        var magic = blob.Slice(0, MagicSize);
        if (!magic.SequenceEqual(Encoding.ASCII.GetBytes(Magic)))
            throw new VaultException(VaultError.Corrupted, "Not a valid vault file (bad magic).");

        int headerLen = BinaryPrimitives.ReadInt32BigEndian(blob.Slice(MagicSize, HeaderLenSize));
        if (headerLen <= 0 || headerLen > MaxHeaderBytes)
            throw new VaultException(VaultError.Corrupted, "Invalid header length.");

        int aadLength = MagicSize + HeaderLenSize + headerLen;
        if (blob.Length < aadLength)
            throw new VaultException(VaultError.Corrupted, "Truncated header.");

        return aadLength;
    }

    private static void RotateBackups(string fullPath, int maxBackups)
    {
        string oldest = $"{fullPath}.bak.{maxBackups}";
        TryDelete(oldest);

        for (int i = maxBackups - 1; i >= 1; i--)
        {
            string src = $"{fullPath}.bak.{i}";
            string dst = $"{fullPath}.bak.{i + 1}";
            if (!File.Exists(src)) continue;
            try { File.Move(src, dst, overwrite: true); }
            catch { /* rotation is best-effort; never fail the save */ }
        }

        try { File.Copy(fullPath, $"{fullPath}.bak.1", overwrite: true); }
        catch { /* same */ }
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { /* ignore */ }
    }
}