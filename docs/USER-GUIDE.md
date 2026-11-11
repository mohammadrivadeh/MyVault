# User guide

## Vaults and protection

Use a writable portable application folder. The default vault and settings live in a `MyVault` folder beside the executable. Choose an existing vault or create another file from the unlock screen. New master passwords require at least 12 characters. There is no password recovery service.

`Ctrl+L` locks; `Ctrl+N` creates an entry; `Ctrl+K` or `Ctrl+F` focuses search. Idle timeout and clipboard clearing are configurable. Session lock/disconnect locks regardless of idle timeout. If copying fails because the clipboard is busy, retry.

## Appearance

Settings offers six palettes with immediate preview. Save retains the choice; Cancel restores the previous palette. System mode uses short transitions when Windows enables animation. Full additionally enables the decorative unlock intro. Off disables those transitions. Windows reduced-motion preference takes priority over all modes. Details appear at widths of at least 1050 device-independent units; narrow windows retain the entry editor and row actions.

## Entries

Use Title, Updated or Created sorting. Smart views show weak passwords, reused passwords, entries changed in the last 30 days, or Trash. Strength labels are estimates, not proof a password is unguessable. Generate opens options for length, classes and ambiguous characters. At least one class must be enabled.

Deleting an active entry moves it to Trash. Select it in Trash and use Restore from Trash. Delete again for permanent removal after confirmation. There is no automatic expiration in this version; old encrypted backups can still contain removed entries.

Optional TOTP accepts a Base32 secret for SHA-1 / six digits / 30-second accounts. Copy one-time code from the details panel. On narrow windows, widen the window to use that action. It uses the computer's clock; keep Windows time synchronized. Secret import from QR/otpauth URLs and other algorithms is not included.

## Backup and import

Encrypted backup copies the whole vault, including Trash, and verifies the destination. Restore validates/decrypts before replacing entries and rotates the current file first. This restore uses the current key; an older-password backup must be opened separately using its original password. Never overwrite the only copy of a backup while experimenting.

CSV import recognizes common column aliases. It skips records missing a title, previews counts and skips matching title (case insensitive), username and URL. No existing entry is overwritten. CSV export is plaintext and incomplete; read the warning before continuing. Use encrypted backup for complete transfers.
