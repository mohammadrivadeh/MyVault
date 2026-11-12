namespace MyVault.Core.Models;

public enum EntryKind { Login, SecureNote, ApiKey, RecoveryCodes }

public sealed class CustomField
{
    public string Name { get; set; } = "";
    public string Value { get; set; } = "";
    public bool IsSecret { get; set; }
}

public sealed class PasswordRevision
{
    public string Password { get; set; } = "";
    public DateTimeOffset ChangedAt { get; set; }
}
