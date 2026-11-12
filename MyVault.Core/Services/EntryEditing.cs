using MyVault.Core.Models;

namespace MyVault.Core.Services;

public static class EntryEditing
{
    public const int MaxHistory = 10;
    public static void SetPassword(VaultEntry entry, string password, int historyLimit = 5)
    {
        if (historyLimit < 0 || historyLimit > MaxHistory) throw new ArgumentOutOfRangeException(nameof(historyLimit));
        if (entry.Password != password)
        {
            if (entry.Password.Length > 0 && historyLimit > 0)
                entry.PasswordHistory.Insert(0, new PasswordRevision { Password = entry.Password, ChangedAt = entry.PasswordChangedAt ?? entry.CreatedAt });
            entry.Password = password;
            entry.PasswordChangedAt = DateTimeOffset.UtcNow;
        }
        if (entry.PasswordHistory.Count > historyLimit) entry.PasswordHistory.RemoveRange(historyLimit, entry.PasswordHistory.Count - historyLimit);
    }
    public static void ApplyTemplate(VaultEntry entry, EntryKind kind)
    {
        entry.Kind = kind;
        // Never erase an existing field when changing a template.
        if (kind == EntryKind.ApiKey && !entry.CustomFields.Any(f => f.Name == "Service")) entry.CustomFields.Add(new CustomField { Name = "Service" });
        if (kind == EntryKind.RecoveryCodes && !entry.CustomFields.Any(f => f.Name == "Recovery codes")) entry.CustomFields.Add(new CustomField { Name = "Recovery codes", IsSecret = true });
    }
}
