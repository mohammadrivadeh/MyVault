# ADR 0002: Coordinate sessions and transactional edits

Status: Accepted

Context: UI mutations preceded writes without rollback. Multiple processes could overwrite one vault; async rekey could overlap session disposal.

Decision: Acquire a companion-file exclusive handle before vault reads/creation. Serialize repository key operations and disposal with a monitor. Use a temporary serialized snapshot in `Update` and restore memory after failure. Use unique temporary write files. Hide UI before closing dialogs and disposing keys on lock. Publish clipboard text atomically with exclusion hints and clear only the owned generation, identified by HWND plus a random private clipboard format. Windows format conversion can alter sequence counters, so sequence-only ownership checks are not used.

Consequences: Background KDF work may briefly delay key disposal, while UI is already hidden. Public mutable models require callers to follow the Update contract. Snapshot strings cannot be reliably zeroed by the managed runtime. Backups are best effort and filesystem durability is bounded by OS/filesystem guarantees. Native desktop integration requires Windows validation.
