# Development and release

Use the existing checkout; each cloud task is isolated. Do not create a worktree unless explicitly requested. Keep real vaults and plaintext exports out of Git.

## Linux cloud

The prepared environment retains SDK 10.0.404 at `/workspace/toolchains/dotnet`:

```sh
source /workspace/setup/MyVault/activate.sh
cd /workspace/MyVault
dotnet restore MyVault.slnx -p:EnableWindowsTargeting=true --artifacts-path /workspace/build/MyVault --locked-mode
dotnet build MyVault.slnx -c Release -p:EnableWindowsTargeting=true --artifacts-path /workspace/build/MyVault --no-restore
dotnet test MyVault.Tests/MyVault.Tests.csproj -c Release --artifacts-path /workspace/build/MyVault --no-build --logger trx
```

If the command sandbox blocks the NuGet shared mutex, use the tool's approved escalated execution mode. Do not disable TLS or checksum verification. The Linux desktop cannot run Windows WPF; successful cross-compilation is not a UI test.

## Windows

Run the README commands. The desktop test needs an interactive Windows desktop and clipboard, uses synthetic values, and must not be run during work using a valuable clipboard. CI uses an ephemeral Windows runner. When dependencies change, run `dotnet restore MyVault.slnx --use-lock-file`, review all package-lock changes, then verify `--locked-mode`. RID-specific publish restores use separate lockfiles under ignored `obj` directories via `Directory.Build.targets`, preserving development locks.

## Publish

```powershell
dotnet publish MyVault.App/MyVault.App.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o artifacts/MyVault-win-x64
Get-FileHash artifacts/MyVault-win-x64/MyVault.App.exe -Algorithm SHA256
```

The checked-in folder profile uses a repository-relative destination and win-x64. Code signing needs a separate certificate and is not configured. CI uploads unsigned build artifacts; it does not create a public release. Validate on a clean Windows machine before distributing.

## Manual release gate

- Unlock old vault and wrong-password/corrupt variants; use only synthetic copies.
- Test all themes at 100%, 150%, 200% DPI, keyboard focus, narrow/wide windows.
- Exercise Ctrl+N/K/F/L, search, filters, sort, Trash/restore/permanent delete.
- Lock Windows with edit, settings, import/export and change-password dialogs open.
- Confirm modal secrets disappear and no stale edit is saved after lock.
- Copy/paste to another app, test history hints, automatic clear and later user copies.
- Test denied writes, read-only application folder, backup restore and old-password backups.
- Verify motion Off and Windows reduced motion, including fast repeated navigation.
- Test Persian/English mixed text. Full localization and RTL layout are not implemented.
