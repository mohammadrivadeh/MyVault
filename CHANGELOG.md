# Changelog

## Unreleased

### Approved phases 1–4

- Executable command/entry palette, exact tag filtering, checkbox bulk edit/delete and unsaved-draft confirmation.
- English/Persian resources and RTL, Windows-following theme, curated accents, text size/density and full preview rollback.
- Login/note/API/recovery templates, encrypted custom fields and per-entry bounded password history.
- Unlocked-session scheduled encrypted backups, strict retention, recent vaults and Chrome/Bitwarden/supported 1Password CSV adapters.
- Local QR-image and otpauth setup import with supported-profile validation.
- Uniform row icon geometry and a dedicated Smart views sidebar section.
- Version-two save migration preserving readable version-one backups; old readers reject new files.
- Expanded portable and Windows workflows, bilingual synthetic layout screenshots, usage/security guides and ADRs 0006–0008.

### Added

- Six semantic color palettes with preview/revert and shared motion preferences.
- Compact row actions, Ctrl+K search, sorting and smart views.
- Two-step code and Trash restore icons on each applicable row; removed the redundant right details panel.
- Five-step authenticator setup guidance, inline feedback for common key mistakes, and a Persian usage guide.
- Encrypted Trash with restore/permanent deletion and configurable password generation.
- Local RFC 6238 TOTP and creation of separate vault files.
- Verified encrypted backup restoration and duplicate-skipping CSV import.
- Portable regression/contrast tests and Windows STA integration CI.
- Architecture, security/user/development guides and five ADRs.

### Fixed

- Bounded untrusted KDF parameters, file size and document structure.
- Exclusive vault write sessions, transactional edit rollback and unique temporary files.
- Session lock closes sensitive modals and hides data before transitions.
- Clipboard ownership-aware clearing and copy failure reporting.
- Settings failure reporting, invalid preference normalization and shutdown/unlock race cleanup.
- Duplicate CSV parser and hardcoded dialog/sidebar colors.
- Machine-specific publish directory and RID contamination of development lockfiles.

### Compatibility

MVLT1 byte layout and cryptographic algorithms remain unchanged. New saves write header/document version 2; older readers reject it. Old backups keep their original master password. CSV remains plaintext and lossy; use encrypted backups for full transfer.
