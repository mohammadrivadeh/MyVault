# Validation record

Date: 2026-11-12 (client timezone Asia/Tehran).

## Current cloud instance

- SDK 10.0.404 on Debian x64.
- Release solution cross-build: passed, zero warnings and zero errors, including compilation of Windows integration tests.
- Core/design suite: 21 passed, zero failed/skipped. Includes real Argon2id/AES-GCM persistence, rekey, wrong password, tampering, rollback under write failure, exclusive leases, original MVLT1 payload compatibility, file limits, backup rotation/restore, CSV, generator, RFC 6238 TOTP vectors and six-palette contrast checks.
- Frozen dependency restore: passed. Publishing initially contaminated development lockfiles with RID-specific dependencies; `Directory.Build.targets` now isolates publish locks under ignored project `obj` directories. Publish followed by locked-mode solution restore passed.
- Windows self-contained single-file cross-publish: passed. Output `/workspace/build/release/MyVault-win-x64/MyVault.App.exe`, verified PE32+ x86-64 Windows GUI binary. SHA-256: `116c26040504f3b379ed2710c00a6121799de7fc64733cbe953ca747f0029ba3`.
- Local Windows integration execution: aborted before tests because Microsoft.WindowsDesktop.App cannot execute on this Linux host. This is not counted as a passing or skipped test.

## Remote Windows validation

CI runs Linux core tests and Windows build/STA integration/publish. The first run exposed contaminated lockfiles, which were corrected. The next run passed Linux tests and Windows build but failed the desktop scenario. Machine-readable test annotations identified a clipboard sequence-counter ownership failure. It was corrected by checking HWND ownership plus a private random marker under the clipboard lock. Final outcome: **passed**, including the real native clipboard regression and modal-lock scenario, on code commit `ad9970e`.

Successful run: https://github.com/mohammadrivadeh/MyVault/actions/runs/43122194177 — Linux core/design job succeeded (21 cases); Windows build, STA integration scenario and self-contained publish job succeeded. This is remote Windows execution evidence, separate from Linux cross-compilation.

Manual DPI/visual, Windows-session-switch, external-app clipboard-history and clean-machine release checks remain required before public distribution. Automated tests do not replace a security audit or those checks. No real vault data was used.

## Compact row actions and guided authenticator setup follow-up

Code commit `5ea2637` passed Linux and Windows CI: https://github.com/mohammadrivadeh/MyVault/actions/runs/43125037667 . The Windows integration scenario now renders the actual vault list at 900-unit width, copies a six-digit code using the bound row button, restores a deleted entry using its row button, and still checks clipboard ownership and modal locking. Setup validation rejects a six-digit code pasted as a setup key and accepts a valid Base32 key. The right details panel was removed, and the editor now includes five-step guidance with a Persian companion guide. Publish succeeded in the same Windows run. Earlier executable checksums above describe the original feature delivery, not this follow-up.

## Approved phases 1–4 delivery

Final code commit `ef7ac6e` passed: https://github.com/mohammadrivadeh/MyVault/actions/runs/43130247900 . Linux ran **41 portable cases, all passed**, covering migration/version consistency, encrypted custom fields/history, retention (including clock rollback), schedule boundaries, source adapters, TOTP vectors/profile rejection, localization coverage and Persian/repeated-password strength behavior in addition to the original regressions. Windows build, the expanded STA integration scenario, and self-contained win-x64 publish all passed.

The Windows scenario executes a command-palette choice, bulk tag editing/deletion, exact tag filtering, canceling an unsaved-editor close, preference preview/revert, real glyph-center checks in English/Persian at 900-unit width, custom secret-field binding, password history, scheduled backup suppression when not due, and decoding a generated local QR. It retains the clipboard-ownership and modal-lock regression checks. Synthetic `rows-en.png` and `rows-fa.png` are uploaded in the Windows artifacts for review. A scenario with many assertions is reported as one Windows test, not inflated into an invented test count.

Release cross-builds had zero warnings/errors. Completed NuGet advisory inspection of all four restored projects reported no problems or vulnerable packages; this is advisory evidence, not an independent audit. See [security assessment](SECURITY-ASSESSMENT.md). Remaining manual clean-Windows, DPI, external-app clipboard/session-switch and signed-distribution gates still apply before public release. No public release or signing was performed.
