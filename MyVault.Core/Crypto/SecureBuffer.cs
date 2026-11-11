using System.Security.Cryptography;

namespace MyVault.Core.Crypto;

/// <summary>
/// Holds a byte buffer (typically a cryptographic key) that is zeroed on dispose.
/// Returned spans are read-only so the caller cannot mutate the buffer.
/// </summary>
public sealed class SecureBuffer : IDisposable
{
    private byte[]? _buffer;

    public SecureBuffer(int size)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(size);
        _buffer = new byte[size];
    }

    /// <summary>
    /// Copies the caller's array into a private buffer.
    /// The caller's array is NOT zeroed — zero it yourself if it held a secret.
    /// </summary>
    public SecureBuffer(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (data.Length == 0) throw new ArgumentException("Buffer cannot be empty.", nameof(data));
        _buffer = (byte[])data.Clone();
    }

    public int Length => _buffer?.Length ?? 0;

    public ReadOnlySpan<byte> Span =>
        _buffer ?? throw new ObjectDisposedException(nameof(SecureBuffer));

    public void Dispose()
    {
        if (_buffer is not null)
        {
            CryptographicOperations.ZeroMemory(_buffer);
            _buffer = null;
        }
        GC.SuppressFinalize(this);
    }
}