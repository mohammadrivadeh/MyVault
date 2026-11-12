# Approved phases 1–4

The user approved these phases. Keep the compact row interface; do not reintroduce a right details panel. Phase 5 (biometrics, sync, browser extension) is excluded.

| Phase | Required delivery | Validation |
| --- | --- | --- |
| 1 | Executable command palette, multi-select bulk actions, tag filter, unsaved editor confirmation | Real Windows command/selection/editor flows |
| 2 | Accent choice, text size, compact/comfortable density, Windows light/dark following, English/Persian UI and RTL | Theme contrast, preferences rollback and bilingual Windows layout |
| 3 | Custom fields, login/note/API/recovery templates, bounded password history | Encrypted persistence, history retention and edit round trips |
| 4 | Scheduled encrypted backups, recent vaults, named CSV source adapters, TOTP QR/otpauth import | Backup retention/failure behavior, import vectors and QR decoding |

All new secrets remain inside authenticated encrypted payloads. Preferences and recent paths are local unencrypted settings. Backups run only while the application has an unlocked session; no background task stores a master password. QR decoding is local and never uploads images.

Work is performed on a feature branch, with core tests, Windows integration CI, documentation and ADRs before merge. UI tests and local Linux cross-build are reported separately. Security assessment documents implementation evidence and limitations, not an invented certification or security score.
