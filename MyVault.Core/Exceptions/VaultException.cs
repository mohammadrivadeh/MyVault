namespace MyVault.Core.Exceptions;

public enum VaultError
{
    WrongPassword,
    Corrupted,
    UnsupportedVersion,
    Io,
    InvalidParameters
}

public sealed class VaultException : Exception
{
    public VaultError Kind { get; }

    public VaultException(VaultError kind, string message)
        : base(message)
    {
        Kind = kind;
    }

    public VaultException(VaultError kind, string message, Exception? inner)
        : base(message, inner)
    {
        Kind = kind;
    }
}