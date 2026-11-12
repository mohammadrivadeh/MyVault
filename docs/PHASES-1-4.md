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

## Implementation and evidence

All four required deliveries are implemented. The first complete code run (`f37cd8d`) passed 37 portable tests and the Windows integration scenario, including publish: https://github.com/mohammadrivadeh/MyVault/actions/runs/43128788526 . The scenario exercises command selection, bulk edit/delete, tag filtering, unsaved-change cancellation, history/custom fields, preference rollback, actual row-icon centering in English/Persian, scheduled encrypted backup and generated QR decoding. Follow-up layout refinements receive a separate CI run before merge. Synthetic English/Persian screenshots are uploaded with Windows test artifacts.

The user additionally requested removal of space-based icon alignment and redesign of Trash/Reused/Recent/Weak navigation. Shared row styles and a dedicated scrollable Smart views section implement those requests. Phase 5 remains excluded. Manual clean-Windows, session-switch, DPI and signed-distribution work is still a release gate; automated scenario success is not an independent security audit.

Final code `ef7ac6e` passed 41 Core/design cases, the expanded Windows scenario and publish: https://github.com/mohammadrivadeh/MyVault/actions/runs/43130247900 . The final retention and Persian/repetition strength corrections are included. All four approved development phases are complete; phase 5 and the documented independent-audit/manual-release work remain excluded.
