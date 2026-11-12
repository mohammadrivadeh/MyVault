# Approved phases 1–4: usage

## Command search and bulk work

Ctrl+K opens commands and active entries; type a command or account name and press Enter or Run. Ctrl+F remains ordinary list search. Checkbox selection is independent of the highlighted entry. Select visible selects all current filtered results; Clear selection clears all, including hidden selections. The selected count always includes selected items outside the current filter. Edit selected applies a category and/or adds one tag in a single transaction. Delete selected confirms moving active entries to Trash and permanently removing selected Trash items. Exact tag filtering combines with the sidebar and text search.

Smart views are a dedicated sidebar section. Categories have their own heading and controls; navigation scrolls on short windows. Row action icons share centered fixed-size cells in both LTR and RTL. The separate right details panel remains removed.

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
