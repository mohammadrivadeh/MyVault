# Architecture

## Projects

- `MyVault.Core`: models, Argon2id derivation, AES-GCM, bounded file parsing, persistence, TOTP, CSV and health services. Portable `net10.0`.
- `MyVault.App`: WPF views and view models, Windows clipboard/session integration, settings, themes and motion. `net10.0-windows`.
- `MyVault.Tests`: portable regression and palette contrast tests.
- `MyVault.Windows.Tests`: an STA integration scenario for resource loading, clipboard ownership and locking a modal editor.

## Session and writes

`MainViewModel` owns one repository, idle lock service and vault screen per unlocked session. Clipboard service belongs to the application and attaches to the main HWND after loading. Repository operations serialize on a private monitor. An exclusive `.lock` handle is acquired before reading or creating a vault and retained until lock/dispose. The companion file is intentionally retained; deleting a live lock file would undermine cross-process exclusion.

UI edits call `VaultRepository.Update`. A temporary serialized snapshot allows memory rollback when validation or persistence fails; its byte buffer is zeroed in `finally`. View-model collections rebuild after both success and rollback. Public models remain mutable for compatibility; callers must use `Update` to obtain transactional behavior.

Writes use a unique sibling temporary file, WriteThrough, flush-to-disk and replacement; rotation preserves up to five previous versions on a best-effort basis. This is not a cross-file transaction or a guarantee against all filesystem/power-loss behavior.

On lock, the UI is hidden immediately, secondary windows are hidden and closed, collections are cleared, then the repository key is disposed. Repository locking serializes disposal against background rekey work. No fade-out displays sensitive content during locking.

## Appearance

`MoonlightGlow.xaml` supplies shared control styles. A last merged palette dictionary overrides semantic `App.*` brushes. `ThemeService` swaps palettes without copying styles. Settings preview applies immediately and reverts on Cancel or session lock. `MotionService` gates transitions with both the user preference and Windows animation settings. Long unlock intro/glow runs only in Full mode.

See ADRs for tradeoffs and format compatibility.
