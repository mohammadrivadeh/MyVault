# ADR 0007: Stable action identifiers and explicit bilingual resources

Status: Accepted

Context: Users approved command search, bulk operations, typography/density/accent controls and Persian RTL. They also requested consistent row icons and a dedicated home for smart views.

Decision: Command palette searches product actions and active entry titles/subtitles; entry data is never translated. Bulk selection uses explicit checkboxes, displays total selected count and applies one repository transaction; deletion confirms whether items move to Trash or are permanently removed. Tag filtering matches an exact case-insensitive tag. Smart views occupy a named vertical sidebar section, separate from user categories, with explicit selected state and a scrollable navigation area. All row icons share geometry and centered glyphs, without whitespace alignment.

Appearance uses semantic resources for text sizes, row padding and curated accents. System theme follows Windows preference; contrast mode takes precedence. English/Persian catalogs use stable hashed message keys, live bindings and RTL at window roots. Passwords and setup keys remain LTR. Labels are separate from stable enum/setting values. Cancel reverts the full appearance preview. Changes in an editor are fingerprinted without retaining a plaintext snapshot; closing prompts unless saving or session locking.

Consequences: Credentials, paths and user category names remain verbatim. Settings and recent paths are local plaintext metadata, not secret storage. Typography is adjustable but icons retain fixed hit targets and glyph sizes. Technical exception details can remain English. Windows integration checks real action bindings, RTL layout and glyph centers and produces synthetic screenshots.
