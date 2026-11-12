# ADR 0008: Unlocked-session backups and local authenticator import

Status: Accepted

Context: Scheduled backup must not require storing a master password. QR and source-specific CSV imports add untrusted input and must not upload setup keys.

Decision: Schedule checks only in an unlocked application session. The repository exports a verified consistent encrypted snapshot serialized against rekey/disposal; background I/O receives ciphertext only. Backups have a vault-path-derived prefix, timestamp and random suffix. Retention deletes only strict application-generated names for that vault and only after the new copy verifies. Report last result in settings. The application need not remain unlocked solely to finish already-exported ciphertext I/O.

Recent successful vault paths are retained locally, capped at eight. CSV source adapters cover generic, Chrome, Bitwarden and supported 1Password CSV columns, preserve credential values, preview counts and never overwrite duplicates. Non-login Bitwarden types outside supported note/login are skipped. CSV remains incomplete and plaintext.

QR import uses pinned ZXing.Net locally on images up to 20 MiB / four megapixels. Decode only QR, accept only otpauth TOTP links using SHA-1 / six digits / 30 seconds, and reject duplicate setup parameters or unsupported algorithms. Explicit setup-link input uses the same parser. QR images themselves contain a secret; users remain responsible for their source files. No camera, network scanning or automatic website activation is introduced.

Consequences: This is not an OS-level backup daemon and does not back up while the app is closed/locked. Source CSV formats are version-dependent; an unsupported export must fail or be explicitly mapped, never guessed silently. Image decoding and packages expand the attack surface and require updated advisories and Windows tests.
