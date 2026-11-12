# ADR 0005: Keep actions on entry rows and guide authenticator setup

Status: Accepted

Context: The wide-window details panel repeated row actions and made TOTP/Trash restoration inaccessible at narrow widths. TOTP terminology did not explain how to obtain or use a setup key.

Decision: Remove the details column and its width-dependent behavior. Keep contextual row icons: username/password/edit for active entries, a clock only for a valid TOTP key, restore only for deleted entries, and delete for both states. Each new icon has a tooltip and accessible name. Use the existing editor for details. Explain authenticator setup in an optional expandable five-step section: start on website, obtain manual key, paste key, save/copy/confirm on website, retain recovery methods. Provide specific feedback for six-digit codes and otpauth links rather than accepting them as keys.

Consequences: No hidden panel must be widened to reach authentication or restore. Local format validation does not prove account activation; the UI states this explicitly. QR scanning, automatic website activation and arbitrary TOTP parameters are still unsupported. A Persian step-by-step guide accompanies the English application UI. Windows integration exercises actual row command bindings at 900-unit width and validates common setup mistakes.
