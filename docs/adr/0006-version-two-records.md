# ADR 0006: Version-two records with bounded encrypted history

Status: Accepted

Context: Custom fields, item kinds and historical passwords must survive edits. Older readers silently discard unknown JSON properties, so additive fields alone cannot safely support downgrade writes.

Decision: Keep the MVLT1 byte layout and AES-256-GCM/Argon2id algorithms, but write header and document version 2. Read versions 1 and 2; upgrade version 1 on its next successful save, preserving the prior file in normal rotated backups. Require header/document version agreement. Older applications must reject version 2. Store login/note/API/recovery kind, up to 100 named custom fields, and at most ten password revisions inside the encrypted document. Each entry chooses history retention (default five; zero disables and clears it on save). Edits roll back together if persistence fails.

Consequences: Existing vaults open without manual conversion. Downgrade requires restoring an old compatible backup and can lose new changes; do not recommend it. A history value deliberately remains sensitive even after the current password changes. Old backups can retain purged history. Models validate collections, limits and enum values. Memory cleanup shortens references but cannot promise managed-string erasure.
