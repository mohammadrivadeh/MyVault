# Security model and limits

Vaults use Argon2id (default 256 MiB, three passes, four lanes) to derive a 256-bit AES-GCM key. Each encryption uses a fresh 96-bit random nonce and 128-bit authentication tag. The plaintext MVLT1 header is authenticated as additional data. Opening requires deriving a key before authenticating that header, so pre-authentication resource limits are essential.

Accepted inputs: vault file at most 64 MiB; header at most 64 KiB; salt 8–64 bytes; Argon2 memory 8 KiB–1 GiB, passes 1–10, lanes 1–16, memory at least eight times lane count. Header and document versions must agree and be one or two; null required collections/fields and duplicate entry/category IDs are rejected. These limits are policy, not claims that every accepted cost is fast on every machine.

The key and temporary byte buffers are zeroed on defined cleanup paths. Managed strings, UI controls, OS clipboard consumers, crash dumps, swap and a compromised OS are outside guarantees of reliable memory erasure. Do not describe this app as formally audited or immune to malware.

Clipboard history/cloud exclusion flags are published with text. These are Windows hints, not protection from all clipboard readers. Clearing checks the HWND owner and a random private generation marker under the native clipboard lock so later user content is preserved. Busy copies report failure instead of claiming success.

Trash remains encrypted inside the vault until permanently removed. Backups may retain deleted data. Changing the master password does not re-encrypt old backups; retain the old password if those backups are needed. TOTP stored alongside passwords shares their compromise boundary; keep independent account recovery methods.

CSV is plaintext and excludes Trash, categories, favorite flags, TOTP and timestamps. It is not a full backup. Untrusted values can be interpreted as formulas by spreadsheet software; import/export preserves credential values rather than silently modifying them. Prefer encrypted backup for full migration.

Version-1 MVLT1 vaults remain readable. The next save writes header/document version 2 with a rotated old-format backup. Older readers reject version 2 instead of silently dropping custom fields/history. Do not downgrade an active vault. The byte layout and cryptographic algorithms are unchanged; see ADR 0006 for the migration contract.

Not included: cloud synchronization, network breach checks, browser integration, biometric key wrapping, automatic updates or cryptographic audit. Report a suspected vulnerability privately to the repository owner; do not upload real vaults or passwords to issues.

See the [current assessment](SECURITY-ASSESSMENT.md) for evidence, advisory inspection and limits including rollback attacks and managed memory.
