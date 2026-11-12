# ADR 0003: Semantic palettes and shared motion policy

Status: Accepted for the initial delivery; subsequent extensions are recorded in [0007-personalization-and-actions](0007-personalization-and-actions.md).

Context: The initial theme service forced one theme and several dialog colors were fixed. Long introductory animation could delay routine access.

Decision: Retain shared WPF templates and override semantic brushes with six palette dictionaries. Apply previews immediately, commit on Save and revert on Cancel/session lock. Test normal text and accent/status contrast at a 4.5:1 minimum. Gate new view/dialog transitions through MotionService, with Windows animation preferences taking priority. Restrict decorative intro to Full mode; never animate sensitive content out during lock.

Consequences: Resource-based controls follow a palette consistently; fixed decorative glyphs/shadows still need visual inspection. No arbitrary custom color editor is exposed because unbounded combinations can break contrast. Full system-theme following, typography scaling, localization and additional palettes remain future work.
