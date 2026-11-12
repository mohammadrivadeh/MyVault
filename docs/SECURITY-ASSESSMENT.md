# Security assessment

Date: 2026-11-12. This is an implementation review with regression evidence, not an independent audit, certification, penetration test or a numerical security score.

## What the implementation provides

| Boundary | Evidence | Limits |
| --- | --- | --- |
| Closed vault confidentiality | Argon2id defaults: 256 MiB, three passes, four lanes; 32-byte salt; AES-256-GCM | Weak or reused master passwords remain vulnerable to offline guessing; 256-bit cipher strength does not mean a human password has 256 bits of entropy |
| File integrity | Fresh random 96-bit nonce and 128-bit tag; header authenticated as AAD; tampering/wrong-password/truncation tests | Does not prevent deletion, ransomware, copying or rollback to an older authentic vault |
| Parsing/resource bounds | File/header/KDF limits, document validation, version agreement; QR/CSV limits | Not fuzzed exhaustively; native Windows image decoding and external libraries remain dependencies |
| Key/session lifetime | Serialized rekey/disposal, explicit lock cleanup, hidden/closed sensitive modals | Managed strings, OS caches, crash dumps and swap cannot be reliably wiped by this application |
| Clipboard | Row-copy actions use history/cloud exclusion hints, owned-generation checks and timed clearing | Malware or another clipboard reader can read before clearing; hints are not a confidentiality boundary |
| Persistence | Exclusive cooperating-instance lease, transaction rollback, flushed sibling-file replacement, tested backup rotation | No protection from an administrator or uncooperative external writer; filesystem/power-loss guarantees vary |
| Backup/history | Snapshots and historical/custom values inside authenticated encryption; bounded history/retention | Old backups keep old master passwords and deleted values; secure deletion on SSD/cloud storage is not guaranteed |
| Imports | Local QR/otpauth parsing with explicit supported parameters; bounded CSV with warnings | CSV and imported QR source images are plaintext secrets and remain the user's responsibility |

## Dependency advisory inspection

Inspected direct and transitive NuGet dependencies for all four projects using `dotnet list MyVault.slnx package --vulnerable --include-transitive --format json --no-restore` with the actual build artifacts properties. The completed report had zero reported problems and no vulnerable packages. Initial inspection used the wrong default assets location and was not counted; it was repeated against the restored graph. This records currently published advisories, not proof that dependencies contain no flaws. The machine-readable result is retained outside the repository at `/workspace/build/vulnerability-report.json`.

## Product readiness

The encryption design uses established primitives and has meaningful tests. The product has not received independent cryptographic review, adversarial desktop testing, a comprehensive dependency/source audit, or a signed release/update supply chain. Do not position it as equivalent to an audited commercial password manager merely because AES-256 is used.

Use a strong unique master passphrase, maintain separate encrypted backups and independent recovery methods, and trust the Windows device. Storing TOTP and passwords in the same vault increases convenience while sharing the vault's compromise boundary. Password-strength labels are approximate heuristics, not a breach database or assurance of resistance to targeted guessing.

Before sensitive production deployment: independent review of file/KDF parsing and lifecycle paths, parser fuzzing, clean-Windows/DPI/session-switch tests, signed distribution, and operational backup/restore exercises. Windows Hello, sync and a browser extension remain phase 5 and are not implemented by this delivery.

## Issues corrected during this delivery

The prior strength heuristic could label uncased Persian letters as empty and overrate short repeated patterns. Uncased letters now participate in the display heuristic and repetition is penalized, with regression cases. The UI explicitly calls the result an estimate; dictionary/common-password and breach screening are still not comprehensive. Backup retention now protects the newly verified snapshot even if earlier filenames have future timestamps.

Clipboard guarantees apply to the application's protected row-copy actions. Manual text selection/copy in editable controls can follow normal Windows control behavior and is not a promise of clipboard-history exclusion.
