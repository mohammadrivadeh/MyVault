# Changelog

## Unreleased

### Added

- Six semantic color palettes with preview/revert and shared motion preferences.
- Compact row actions, Ctrl+K search, sorting and smart views.
- Two-step code and Trash restore icons on each applicable row; removed the redundant right details panel.
- Five-step authenticator setup guidance, inline feedback for common key mistakes, and a Persian usage guide.
- Encrypted Trash with restore/permanent deletion and configurable password generation.
- Local RFC 6238 TOTP and creation of separate vault files.
- Verified encrypted backup restoration and duplicate-skipping CSV import.
- Portable regression/contrast tests and Windows STA integration CI.
- Architecture, security/user/development guides and four ADRs.

### Fixed

- Bounded untrusted KDF parameters, file size and document structure.
- Exclusive vault write sessions, transactional edit rollback and unique temporary files.
- Session lock closes sensitive modals and hides data before transitions.
- Clipboard ownership-aware clearing and copy failure reporting.
- Settings failure reporting, invalid preference normalization and shutdown/unlock race cleanup.
- Duplicate CSV parser and hardcoded dialog/sidebar colors.
- Machine-specific publish directory and RID contamination of development lockfiles.

### Compatibility

MVLT1 encryption remains unchanged. Older versions may display Trash entries because they ignore the additive deletion timestamp. Old backups keep their original master password. CSV remains plaintext and lossy; use encrypted backups for full transfer.
