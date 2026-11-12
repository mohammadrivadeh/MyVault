# MyVault

A portable Windows password vault built with .NET 10 and WPF. Vault contents stay local and are encrypted with AES-256-GCM using an Argon2id-derived key. No account or network service is needed.

## Features

- Entries, categories, favorites, tags and search (`Ctrl+K` / `Ctrl+F`).
- Six color palettes: MoonlightGlow, Graphite, Nord, Forest, Ivory and HighContrast.
- Live theme preview with Cancel rollback; System / Full / Off motion settings respecting Windows reduced motion.
- Compact per-entry action icons, sorting, Weak / Reused / Recent views and encrypted Trash with manual restore/permanent deletion.
- Configurable cryptographic password generator and guided optional two-step verification (TOTP: SHA-1, six digits, 30 seconds).
- Separate vault files; create or choose another vault from the unlock screen.
- Encrypted backup export and verified restore for backups using the current key.
- CSV import with alias support and duplicate skipping; explicit plaintext export warnings.
- Idle and Windows session locking; timed clearing of only the application's clipboard generation.

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

MyVault یک مدیر رمز عبور محلی برای ویندوز است. این نسخه تم‌های متنوع، تنظیم حرکت، آیکون‌های عملیات هر ردیف، تولید رمز قابل‌تنظیم، TOTP، سطل بازیافت و بازیابی بکاپ دارد. اطلاعات Vault رمزگذاری می‌شوند؛ فایل CSV رمزگذاری نمی‌شود. قبل از ارتقا از Vault خود بکاپ بگیرید. اجرای رابط و تست‌های دسکتاپ به ویندوز نیاز دارد. برای راه‌اندازی کد دومرحله‌ای، [راهنمای فارسی قدم‌به‌قدم](docs/fa/TOTP.md) را ببینید.
