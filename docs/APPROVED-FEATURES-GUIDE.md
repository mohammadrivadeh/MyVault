# Approved phases 1–4: usage

## Search and navigation

Ctrl+F focuses list search. Use the row icons for entry operations; command palette and bulk selection controls are removed. Categories appear above Smart views in the sidebar. Exact tag filtering combines with ordinary search and the selected view. Custom fields are labeled name/value cards: the name describes the information (for example Account number), and the value is what you store. The checkbox hides the value on screen; all fields remain encrypted.

## Appearance and language

Settings offers System or six fixed palettes, curated accent colors, text size 12–18 and Compact/Comfortable rows. System follows Windows light/dark preference. High-contrast mode keeps its palette's contrast colors instead of applying a custom accent. en selects English; fa selects Persian and RTL. Inputs containing passwords/setup keys stay LTR. Existing category names, entry content and paths are not translated. Preview applies immediately; Cancel restores all original appearance choices. Save persists them. Product labels are translated; technical exception details and brand/algorithm names may remain English.

## Item templates and history

Choose Login, SecureNote, ApiKey or RecoveryCodes in the entry editor. Changing kind does not erase existing content. API and recovery templates supply initial custom-field names; add/remove named fields and mark secret values to mask them. All fields, including non-masked ones, are encrypted in the vault. The secret checkbox affects display, not whether a value is encrypted.

Password history retains five previous values by default, with a maximum of ten. Select zero to clear/disable on save. Use in editor loads an old password into the draft; saving is still required. Clear history affects the draft until saved, and old backups can retain values. Closing a changed editor prompts; locking the session hides and closes it without permitting a prompt to block security cleanup.

## Backups, recent vaults and import

Choose a backup folder, a nonzero interval in minutes and retention in Settings. A due backup runs on unlock or a 30-second scheduler check while unlocked. Closing/locking stops future checks; an already-exported encrypted snapshot may finish writing. Last backup result is visible in Settings. Files are verified before rotation, and unrelated user files are never selected for retention deletion. Keep backups on separate storage and test restoration.

Recent successful vault paths appear on the unlock screen, up to eight. Paths and display preferences are plaintext local metadata; keys and passwords are not stored there. Selecting a missing/moved vault shows an error instead of overwriting it.

CSV sources: Auto/Generic, Chrome, Bitwarden and supported 1Password CSV. Use login exports with the expected header names; Bitwarden login/note types are supported. Unsupported types are skipped. Compatible TOTP fields can be imported; unsupported authenticator parameters reject import. Preview and duplicate skipping remain in effect. CSV export excludes custom fields, history, categories, favorites, TOTP, timestamps and Trash: use encrypted backup for a full copy.

In authenticator setup, either paste a manual Base32 key, use an otpauth link or import a local QR image. Supported profiles remain SHA-1 / six digits / 30 seconds. Images are decoded locally and never uploaded; source QR images contain setup secrets, so store or remove them appropriately. Saving locally still does not activate the website's 2FA: confirm a fresh code on the website and keep recovery codes.

## Upgrade behavior

Version 1 vaults open normally and upgrade to header/document version 2 on save, with a rotated old-format backup. Old app versions reject the new file rather than dropping custom fields/history. Do not downgrade by opening the new file in an old app. The byte layout and encryption/KDF algorithms remain unchanged.

## Interface simplification

The command palette and bulk selection/edit/delete interface have been removed at the user's request. Ctrl+F remains normal search. Entry operations use their row icons. Categories appear above Smart views. Custom fields use separate cards with explicit name/value labels, an example name, and a display-masking checkbox; all values remain encrypted. Earlier sections describing the removed palette/bulk controls are historical and superseded by this revision.
