# MyVault implementation plan

This work is delivered in reviewable commits on a feature branch. Existing MVLT1 vaults remain readable. No production vault or credentials are used in tests.

| Phase | Deliverable | Acceptance |
| --- | --- | --- |
| 1 | Core regression suite and CI | Actual crypto/storage/import tests pass; Windows builds |
| 2 | Security and persistence | Bounded input, coordinated lock, owned clipboard, transactional edits |
| 3 | Design system | Six palettes, live preview/revert, shared motion and reduced-motion support |
| 4 | Daily workflow | Compact row actions, keyboard search, sorting and smart views |
| 5 | Vault capabilities | Generator controls, TOTP, trash and import duplicate policy |
| 6 | Documentation and release | ADRs, user/developer docs, repeatable build and validation report |

Windows interactive validation must be reported separately from Linux compilation and core tests. A configured CI workflow is not evidence of a completed CI run. Optional future product ideas (cloud sync, browser extension, attachments, custom templates, password history) need separate threat models and are not prerequisites for this release.

## Delivery status

- Phases 1–2: implemented; portable regression tests pass. The Windows desktop CI scenario passed on commit `ad9970e`.
- Phases 3–5: implemented for the concrete release scope above. Palettes pass automated contrast checks; the Windows desktop CI scenario passed. Manual release checks remain separate.
- Phase 6: architecture, security, developer/user guides and four ADRs written. Cross-published unsigned win-x64 executable verified as a PE32+ GUI binary. No public release has been created.
- Git integration: feature branch pushed. Final code CI passed on both Linux and Windows; merge uses the tested code plus documentation.

## Later product work, not represented as complete

- Full command palette with executable commands (current Ctrl+K focuses search).
- Passphrase generation, persisted generator presets and stronger guessability estimation.
- System light/dark following, custom accent selection, density/font controls and full Persian localization/RTL.
- Custom templates/fields, API-key expiry reminders, password history and review center.
- Scheduled backups, recent-vault list, bulk operations, QR/otpauth import and attachments.
- Cloud synchronization, browser extension, biometric key wrapping and signed automatic updates.

These need individual acceptance criteria and, where sensitive data or new storage semantics are involved, additional ADRs. Do not mark the entire product wishlist complete based on this release.
