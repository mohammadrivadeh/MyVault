# MyVault

A portable Windows password vault built with .NET 10 and WPF. Vault contents stay local and are encrypted with AES-256-GCM using an Argon2id-derived key. No account or network service is needed.

## Features

- Compact row actions with aligned icons; separate Library, Smart views and Categories navigation.
- Ctrl+K commands/entry palette, Ctrl+F search, exact tag filtering and transactional bulk operations.
- Six palettes plus Windows-following System, curated accents, text size/density, live preview/revert and reduced motion.
- English/Persian UI with RTL; credentials and paths remain verbatim.
- Login, secure-note, API-key and recovery-code templates with encrypted custom fields and bounded password history.
- Guided TOTP setup with manual key, local QR image or otpauth link import (SHA-1, six digits, 30 seconds).
- Encrypted Trash, smart views, clipboard protection and idle/Windows-session locking.
- Verified encrypted backup export/restore, unlocked-session scheduled backups and recent vaults.
- Generic/Chrome/Bitwarden/supported 1Password CSV import with duplicate skipping and explicit plaintext export warnings.
- Tested version-1-to-version-2 save migration; old app versions reject new files rather than dropping new fields.

See the [approved features guide](docs/APPROVED-FEATURES-GUIDE.md), [phase checklist](docs/PHASES-1-4.md) and [security assessment](docs/SECURITY-ASSESSMENT.md).

## Develop

Install a .NET 10 SDK. `global.json` accepts the latest installed .NET 10 feature band.

```sh
dotnet restore MyVault.slnx --locked-mode
dotnet build MyVault.slnx -c Release --no-restore
dotnet test MyVault.Tests/MyVault.Tests.csproj -c Release --no-build
dotnet test MyVault.Windows.Tests/MyVault.Windows.Tests.csproj -c Release --no-build
dotnet run --project MyVault.App
```

The application and desktop tests require Windows. Linux supports core tests and cross-compilation with `-p:EnableWindowsTargeting=true`; it cannot execute WPF. See [development](docs/DEVELOPMENT.md), [user guide](docs/USER-GUIDE.md), [security](docs/SECURITY.md), [architecture](docs/ARCHITECTURE.md), [decisions](docs/adr/README.md) and [validation](docs/VALIDATION.md).

## فارسی

MyVault یک مدیر رمز عبور محلی برای ویندوز است. این نسخه رابط فارسی و راست‌به‌چپ، جست‌وجوی فرمانی، عملیات گروهی، تنظیم ظاهر، فیلد سفارشی، تاریخچه محدود رمز، راهنمای TOTP و ورود QR، بکاپ زمان‌بندی‌شده و مخزن‌های اخیر دارد. اطلاعات Vault رمزگذاری می‌شوند؛ فایل CSV رمزگذاری نمی‌شود. قبل از ارتقا از Vault خود بکاپ بگیرید. اجرای رابط و تست‌های دسکتاپ به ویندوز نیاز دارد. برای راه‌اندازی کد دومرحله‌ای، [راهنمای فارسی قدم‌به‌قدم](docs/fa/TOTP.md) را ببینید.

[راهنمای فارسی امکانات جدید](docs/fa/FEATURES.md)
