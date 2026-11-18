# Nexus Home MVP Database Notes

## Canonical database model

EF Core migrations are the source of truth for the PostgreSQL schema. The canonical MVP model currently includes `users`, `homes`, `memberships`, `rooms`, `devices`, `device_telemetry`, `invitations`, `permissions`, `settings`, `ai_action_audit`, and `device_commands`. Automation tables are intentionally excluded from the MVP.

- `schema_from_migrations.sql` is the generated idempotent migration script for deployment/review. Review it before applying to an existing database.
- `Docs/NexusHome_ERD_MVP.drawio` is the editable ERD corresponding to the current migration model. `Docs/NexusHome_ERD_MVP.mmd` is the source used for the rendered PNG.

## Important schema behavior

- Each home can have at most one `owner` membership, enforced by a partial unique index. The database does not enforce that every home always has an owner; home creation/application logic must create its owner membership in the same operation.
- Membership roles are constrained to `owner`, `member`, and `guest`. Device-control authorization for members and guests remains an application authorization responsibility.
- A device has a required home and an optional room. A composite foreign key `FK_devices_rooms_home_room` from `devices (home_id, room_id)` to `rooms (home_id, id)` with `MATCH SIMPLE` enforces that when a room is assigned, it belongs to the same home. `room_id` remains nullable (device may have no room).
- `device_telemetry` is a TimescaleDB hypertable partitioned by `timestamp`. Its primary key is `(id, timestamp)`, satisfying the hypertable unique-key requirement. The `(device_id, timestamp)` index is non-unique, so multiple metrics for a device at one timestamp are allowed.
- `devices.external_id` is globally unique. The redundant unique index on `(home_id, external_id)` was removed in migration `20261016003708_RemoveRedundantDeviceExternalIdIndex`; the global unique index already guarantees uniqueness for every home/external ID pair.
- `schema_from_migrations.sql` is generated from migrations and must not be edited by hand. Raw SQL statements in migrations must include statement terminators so the generated script remains executable.
- **Device reported state**: The `devices` table now includes `reported_state` (JSONB), `reported_at` (timestamptz), and `state_version` (bigint, default 0) columns to store the latest device-reported state durably. Only trusted device reports update these fields; requested command values do not.
- **Soft device removal**: Devices have a nullable `removed_at` column for soft deletion. Hard deletion is prevented for devices with related history by changing foreign keys on `device_telemetry`, `ai_action_audit`, and `device_commands` to `ON DELETE RESTRICT`. The `devices.home_id` FK is also `RESTRICT` to prevent cascade removal of devices when a home is deleted.
- **Permission effect**: The `permissions` table includes an `effect` column (`grant` or `deny`) with a check constraint. Existing rows are migrated to `grant`. The legacy `permission_type` values are preserved; application authorization maps their behavior.
- **Settings uniqueness**: The `settings` table has a unique index on `user_id`, enforcing one row per user (personal settings). Home-level settings are a future decision.
- **Device commands**: The `device_commands` table stores durable command lifecycle records with idempotency support (unique composite index on `requested_by_user_id, idempotency_key`), status tracking, and structured error fields. A composite foreign key `FK_device_commands_devices_home_device` from `device_commands (home_id, device_id)` to `devices (home_id, id)` ensures the referenced device belongs to the command's home. Foreign keys to `devices` and `homes` use `RESTRICT` to preserve command history.
- **User deletion and command history**: The `device_commands.requested_by_user_id` FK uses `ON DELETE CASCADE`. Deleting a user removes their command history. This is intentional for GDPR/privacy compliance; if command history must be preserved, use soft-deletion on users instead of hard deletion. Alternatives (`RESTRICT` or `SET NULL`) were considered but rejected: `RESTRICT` would block user deletion, `SET NULL` would orphan commands and break audit trails.

## Local development and deployment

1. Copy `.env.example` to `.env` and set a strong local `POSTGRES_PASSWORD`.
2. Start PostgreSQL/TimescaleDB with Docker Compose. Compose intentionally fails if the password is missing; there is no committed default password.
3. Set `ConnectionStrings__NexusHome` for the API and run it in Development to apply pending migrations automatically.
4. Production does not apply migrations automatically at API startup. Generate and review the idempotent migration script, then apply it through the deployment process using a migration identity with schema permissions.

Never commit `.env`, real credentials, or production connection strings.

## Verification

- Solution build passed with 0 warnings and 0 errors after adding migration SQL terminators and making hypertable creation work inside EF's idempotent-script guards.
- The idempotent deployment script is generated from the compiled EF Core migrations.
- The editable ERD parses as XML and contains 11 entity nodes and 15 relationship edges, including the separate grantor and grantee links from memberships to permissions, and the new device_commands relationships.
- `Docs/NexusHome_ERD_MVP.png` has been regenerated from the migration-aligned Mermaid ERD source and visually inspected. The draw.io desktop exporter itself still terminates with signal 6 in this environment.
- Before migration `20261016003708_RemoveRedundantDeviceExternalIdIndex` was added, generated scripts were applied successfully to clean databases in a temporary TimescaleDB container, and the idempotent script succeeded on a second run. Those checks covered the prior two-migration state; the updated four-migration idempotent script has been regenerated but still needs database application validation.
- TimescaleDB prints advisory warnings that `metric_name` and `metric_unit` could use `TEXT`; the scripts still apply successfully, and the bounded string columns remain aligned with the EF model.
- EF CLI reports version 10.0.10 while the runtime is 10.0.12. This warning did not prevent build or script generation; update the local tool when convenient for version parity.

## Deferred decisions / known limitations

- **Duplicate settings check**: Migration does not auto-resolve duplicate `settings.user_id` rows; they must be resolved manually before the unique index can be created. The migration will fail if duplicates exist.
- **Telemetry NULL metric_value**: The migration fails with a clear error if any `device_telemetry.metric_value` is NULL. No default value is applied. Resolve NULLs (delete or update) before applying the migration. This prevents inventing zero readings.
- **User deletion cascade**: `device_commands.requested_by_user_id` uses `ON DELETE CASCADE`. If preserving command history after user deletion becomes a requirement, consider soft-deletion on users or changing to `RESTRICT` with application-level handling.
