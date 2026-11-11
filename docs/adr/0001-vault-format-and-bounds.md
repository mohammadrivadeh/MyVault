# ADR 0001: Preserve MVLT1 and bound untrusted work

Status: Accepted

Context: Existing vaults use an authenticated JSON header and AES-GCM payload. KDF parameters must be consumed before authentication and previously had only lower bounds.

Decision: Retain the existing algorithm and byte layout. Validate exact format/document version, bounded file/header/salt/KDF costs and required model structure. New entry fields are additive JSON properties. Protect compatibility with regression tests; avoid inventing a new cipher or migration during UI development.

Consequences: Existing default vaults remain supported. Unusual files exceeding documented bounds are rejected. Old app versions do not understand Trash and may display those entries; downgrade is not supported as a semantic round trip. A future incompatible schema needs a new version and tested migration.
