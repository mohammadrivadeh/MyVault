# ADR 0004: Local TOTP, manual Trash and non-destructive CSV import

Status: Accepted for the initial delivery; subsequent extensions are recorded in [0008-local-backups-and-import](0008-local-backups-and-import.md).

Context: The entry schema already had an unused TOTP secret. Permanent deletion was immediate after confirmation. CSV cannot represent all vault metadata.

Decision: Implement offline RFC 6238 SHA-1/six-digit/30-second TOTP with reference-vector tests. Store deletion timestamps inside the encrypted document; restore manually and explicitly confirm permanent deletion. Exclude Trash from health and CSV export. Import skips title/username/URL duplicates and never silently overwrites existing entries. Full transfers use verified encrypted backups; restoration accepts only data decryptable with the active key.

Consequences: TOTP and passwords share one compromise boundary. Trash has no automatic retention purge in this release and old backups retain previous data. CSV is intentionally lossy and plaintext; spreadsheet formula interpretation is documented rather than altering credential values. Arbitrary TOTP algorithms, QR import, scheduled backup, custom templates, attachments and cloud sync require separate design work.
