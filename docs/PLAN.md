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

## Approved follow-up phases 1–4

The previously deferred command palette, bulk actions, appearance controls/Persian, templates/custom fields/history, scheduled backups/recents/source import and QR import are now implemented. See [the approved checklist](PHASES-1-4.md), [usage guide](APPROVED-FEATURES-GUIDE.md) and ADRs 0006–0008. Current security boundaries are recorded in [the assessment](SECURITY-ASSESSMENT.md).

## Remaining product work

Passphrase generation, persisted generator presets, stronger guessability estimation, API-key expiry reminders, review center, attachments, cloud synchronization, browser extension, biometric key wrapping and signed automatic updates are not represented as complete. The user approved phases 1–4; phase 5 remains outside this delivery.
