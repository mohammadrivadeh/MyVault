# User guide

## Vaults and protection

Use a writable portable application folder. The default vault and settings live in a `MyVault` folder beside the executable. Choose an existing vault or create another file from the unlock screen. New master passwords require at least 12 characters. There is no password recovery service.

`Ctrl+L` locks; `Ctrl+N` creates an entry; `Ctrl+K` or `Ctrl+F` focuses search. Idle timeout and clipboard clearing are configurable. Session lock/disconnect locks regardless of idle timeout. If copying fails because the clipboard is busy, retry.

## Appearance

Settings offers six palettes with immediate preview. Save retains the choice; Cancel restores the previous palette. System mode uses short transitions when Windows enables animation. Full additionally enables the decorative unlock intro. Off disables those transitions. Windows reduced-motion preference takes priority over all modes. All entry actions are available directly on each row at every window width; there is no separate details panel. Double-click or use the edit icon to view and edit entry fields.

## Entries

Use Title, Updated or Created sorting. Smart views show weak passwords, reused passwords, entries changed in the last 30 days, or Trash. Strength labels are estimates, not proof a password is unguessable. Generate opens options for length, classes and ambiguous characters. At least one class must be enabled.

Deleting an active entry moves it to Trash. Use the Restore from Trash icon on its row. Delete again for permanent removal after confirmation. There is no automatic expiration in this version; old encrypted backups can still contain removed entries.

TOTP means a time-based one-time password: a six-digit two-step verification code that changes every 30 seconds, like Google Authenticator. It is generated locally from the account’s setup key, not delivered by SMS. The optional field accepts a Base32 secret for SHA-1 / six digits / 30-second accounts. Copy the code using the clock icon on the entry row; this icon appears only when the entry has a valid key. It uses the computer's clock; keep Windows time synchronized. Local QR images and supported otpauth links can be imported in the editor. Other algorithms/digit counts/periods remain unsupported.

### Set up an authenticator key

1. On the website, open Security → Two-step verification / 2FA and select Authenticator app.
2. Next to its QR code, choose Can't scan? / Enter key manually and copy the longer setup key.
3. In MyVault's entry editor, expand Two-step verification and paste that key in step 3. Do not paste a six-digit login code, account password, QR image into the manual-key field. Use the dedicated QR/link import controls instead. Inline feedback explains common mistakes.
4. Save the entry, use its clock icon to copy a code, and enter that code on the website to complete activation. Local key-format acceptance and saving alone do not enable 2FA on the website.
5. Keep the website's recovery codes safely outside this vault. Removing a key in MyVault does not disable the website's 2FA; prepare an alternative authenticator or recovery method first.

[راهنمای فارسی قدم‌به‌قدم](fa/TOTP.md)

## Backup and import

Encrypted backup copies the whole vault, including Trash, and verifies the destination. Restore validates/decrypts before replacing entries and rotates the current file first. This restore uses the current key; an older-password backup must be opened separately using its original password. Never overwrite the only copy of a backup while experimenting.

CSV import recognizes common column aliases. It skips records missing a title, previews counts and skips matching title (case insensitive), username and URL. No existing entry is overwritten. CSV export is plaintext and incomplete; read the warning before continuing. Use encrypted backup for complete transfers.

## Approved phases 1–4

The [extended guide](APPROVED-FEATURES-GUIDE.md) covers command search, bulk operations, Persian/RTL, text/density/accent settings, custom fields, history, scheduled backups, recent vaults, source CSV adapters and format upgrades.
