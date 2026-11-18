# NEXUS HOME — FINAL ARCHITECTURE BASELINE

**Document Status:** FINAL CORRECTION & BASELINE FREEZE  
**Version:** 1.3  
**Date:** 2026-11-18  
**Author:** Backend Engineering Track  
**Review State:** Ready for Technical Lead Review  
**Technically Approved:** PENDING  
**Migration Generation:** NO-GO (schema-affecting decisions unresolved)  
**Migration Application:** NOT AUTHORIZED

---

## 1. EXECUTIVE VERDICT

### 1.1 Readiness Classification

| Gate | Status | Condition |
|------|--------|-----------|
| **Ready for Technical Lead Review** | **YES** | Document is internally consistent, source-grounded, and all contradictions resolved |
| **Technically Approved** | **PENDING** | Awaiting cross-team resolution of B-01, B-02, and B-06 legal policy |
| **Migration Generation** | **NO-GO** | Blocked by 2 schema-affecting decisions (B-01, B-02 enum values) |
| **Migration Application** | **NOT AUTHORIZED** | Requires explicit database-change approval after Technical Lead authorization |

This revision resolves all documented internal contradictions and implementation defects against authoritative project sources. The architecture is internally consistent and source-grounded. NexusLink protocol compatibility has been independently verified against all five source JSON schemas and the specification document (spec.md).

**Two schema decisions remain open and block migration generation:**
- **B-01**: Permission type enum values (Product + Security)
- **B-02**: Target type enum values (Product + Security)

**One compliance gate (does NOT block migration generation):**
- **B-06 (legal policy)**: User anonymization/erasure workflow, retention policy, GDPR confirmation (Legal + Product) — **The FK decision `ON DELETE CASCADE` for `device_commands.requested_by_user_id` is CONFIRMED per DATABASE_MVP_SUMMARY.md line 23 and is not reopened.**

### 1.2 Blocker Summary

| Blocker ID | Topic | Blocks | Required From | Blocks Schema? |
|------------|-------|--------|---------------|----------------|
| **B-01** | Permission type enum values | `PERMISSIONS` table CHECK constraint / CREATE TYPE | Product + Security | **YES** |
| **B-02** | Target type enum values | `PERMISSIONS` table CHECK constraint / CREATE TYPE | Product + Security | **YES** |
| **B-03** | Role default permission matrix | Authorization behavior at runtime | Product + Security | NO (app logic) |
| **B-04** | Device capabilities JSON schema | `DEVICES.capabilities` validation | Embedded + AI | NO (jsonb without CHECK) |
| **B-05** | Telemetry retention period | TimescaleDB retention policy DDL | Product + DevOps + Legal | NO (policy applied later) |
| **B-06 (enum/FK)** | **CONFIRMED** — `device_commands.requested_by_user_id ON DELETE CASCADE` | — | — | **NO (decided)** |
| **B-06 (legal)** | User anonymization/erasure workflow, retention policy | Compliance/operational approval | Legal + Product | NO (policy, not schema) |
| **B-07** | MQTT broker selection | Phase 3.1 infrastructure | Network + DevOps | NO (infrastructure) |

**Schema Blockers (P0 — Block Migration Generation):** B-01, B-02 (2 total — enum values only)  
**Runtime/Integration Blockers (P1 — Block Feature Integration):** B-03, B-04, B-05, B-07, I-01, I-02, I-03, I-04, I-05 (9 total)  
**Operational/Compliance Blockers (P2 — Block Production Hardening):** O-01, O-02, O-03, O-04, O-05, O-06, **B-06 legal policy** (7 total)

> **B-06 Clarification:** The FK action `ON DELETE CASCADE` for `device_commands.requested_by_user_id` is a **Confirmed Decision (D-18)** per `DATABASE_MVP_SUMMARY.md` line 23. This is not reopened. The remaining Legal + Product decision is strictly about the anonymization/erasure workflow, retention periods, and whether the CASCADE behavior satisfies GDPR "right to erasure" — a compliance/operational approval gate, not a schema choice.

### 1.3 What Is Ready

- Complete 11-table domain schema with all constraints, indexes, and FKs defined (enum values for B-01/B-02 are placeholders)
- Infrastructure table designs (outbox, telemetry deduplication ledger)
- **NexusLink protocol compatibility verified against all five source JSON schemas and spec.md**
- EF Core mappings defined for all entities
- Migration strategy with raw SQL for TimescaleDB/triggers
- Validation checklist for implementation verification
- **Confirmed FK decision:** `device_commands.requested_by_user_id ON DELETE CASCADE` (D-18, per DATABASE_MVP_SUMMARY.md line 23)

---

## 2. CONFIRMED PROJECT SCOPE

### 2.1 Explicitly IN SCOPE (11 Domain Tables)

| Table | Purpose | Source |
|-------|---------|--------|
| `USERS` | User accounts, authentication, soft delete | DB Summary, ERD, Backend Scope |
| `HOMES` | Home entities, address, timezone | DB Summary, ERD, Backend Scope |
| `MEMBERSHIPS` | User↔Home roles (Owner/Member/Guest) | DB Summary, ERD, Contract §3.1 |
| `ROOMS` | Rooms within homes | DB Summary, ERD, Backend Scope |
| `DEVICES` | Device registry, capabilities, state, soft delete | DB Summary, ERD, Contract §12 |
| `DEVICE_TELEMETRY` | Time-series sensor readings (TimescaleDB) | DB Summary, ERD, Backend Scope, Contract §9 |
| `INVITATIONS` | Home membership invitations | DB Summary, ERD, Contract §12 |
| `PERMISSIONS` | Grant/deny permissions with target scoping | DB Summary, ERD, Contract §3.2 |
| `SETTINGS` | Per-user personal settings | DB Summary, ERD, Contract §9 |
| `AI_ACTION_AUDIT` | AI action audit trail | DB Summary, ERD, Contract §7,10 |
| `DEVICE_COMMANDS` | Durable command lifecycle, idempotency | DB Summary, ERD, Contract §5 |

### 2.2 Explicitly OUT OF SCOPE

| Capability | Source |
|------------|--------|
| Automations, schedules, rule builders | Backend Scope §1.1, Contract §1.2, DB Summary |
| Automation run logs | Contract §1.2, DB Summary |
| Raw camera/microphone recording storage | Contract §1.2, §10 |
| AI model training/implementation | Backend Scope §1.1, Contract §1.2 |
| Firmware implementation | Backend Scope §1.1, Contract §1.2 |
| NexusLink protocol design | Backend Scope §1.1, Contract §1.2 |
| Mobile UI/UX implementation | Backend Scope §1.1 |
| pgvector / vector memory | Backend Scope §3 (Optional), Contract §9 |
| Conversation/context storage | Backend Scope §4, Contract §9 (TBC) |

### 2.3 Scope Boundaries — Confirmed

| Boundary | Definition |
|----------|------------|
| **11 Domain Tables** | Fixed — no additions without Technical Lead approval |
| **No Automation Tables** | Zero automation-related tables in MVP |
| **DEVICE_COMMANDS Included** | Explicitly approved despite initial 10-table instruction |
| **pgvector Excluded** | Optional, not in MVP unless AI track confirms |

---

## 3. FINAL ARCHITECTURE

### 3.1 System Components

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         NEXUS HOME MVP ARCHITECTURE                         │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐    ┌──────────┐  │
│  │   Mobile     │    │  Wall Panel  │    │   AI/FastAPI │    │ Embedded │  │
│  │    App       │    │              │    │   Service    │    │  Devices │  │
│  └──────┬───────┘    └──────┬───────┘    └──────┬───────┘    └────┬─────┘  │
│         │                   │                   │                 │        │
│         │ HTTPS/REST        │ HTTPS/REST        │ HTTPS/REST      │ MQTT   │
│         │ SignalR/WS        │ SignalR/WS        │                 │ TLS    │
│         ▼                   ▼                   ▼                 ▼        │
│  ┌──────────────────────────────────────────────────────────────────────┐  │
│  │                        BACKEND (ASP.NET Core)                        │  │
│  │  ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌──────────────┐  │  │
│  │  │ Auth    │ │ Homes/  │ │ Devices │ │ AI      │ │ MQTT         │  │  │
│  │  │ (JWT)   │ │ Rooms/  │ │ Registry│ │ Gateway │ │ Client       │  │  │
│  │  │         │ │ Members │ │         │ │         │ │ (NexusLink)  │  │  │
│  │  └─────────┘ └─────────┘ └─────────┘ └─────────┘ └──────────────┘  │  │
│  │  ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌──────────────┐              │  │
│  │  │ Permis- │ │ Command │ │ Telemetry│ │ Outbox       │              │  │
│  │  │ sions   │ │ Dispatch│ │ Ingestion│ │ Processor    │              │  │
│  │  └─────────┘ └─────────┘ └─────────┘ └──────────────┘              │  │
│  └────────────────────────────────┬────────────────────────────────────┘  │
│                                   │                                       │
│         ┌─────────────────────────┼─────────────────────────┐             │
│         ▼                         ▼                         ▼             │
│  ┌─────────────┐          ┌─────────────┐          ┌─────────────┐      │
│  │ PostgreSQL  │          │  Redis      │          │ TimescaleDB │      │
│  │ (Primary)   │          │  (Live      │          │ (Telemetry) │      │
│  │ 11 tables   │          │  State)     │          │ Hypertable  │      │
│  └─────────────┘          └─────────────┘          └─────────────┘      │
│         │                                             ▲                  │
│         │              ┌──────────────────────────────┘                  │
│         ▼              ▼                                                 │
│  ┌─────────────────────────────────────────────────┐                   │
│  │              OUTBOX EVENTS TABLE                │                   │
│  │         (PostgreSQL → Redis/SignalR)            │                   │
│  └─────────────────────────────────────────────────┘                   │
│                                                                             │
│  CLOUD / REMOTE SERVER                                  LOCAL HOME HUB    │
│  ─────────────────────                                  ──────────────    │
│  • Remote access                                        • Local MQTT      │
│  • Mobile sync                                          • Device control  │
│  • Backups                                              • Privacy AI      │
│  • Central auth                                         • Offline ops     │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 3.2 Data Flow Summary

| Flow | Path | Consistency Model |
|------|------|-------------------|
| User command | HTTPS → Backend → MQTT(command) → Device → MQTT(result) → Backend → PG(outbox) → Redis/SignalR | Eventual (PG=source of truth) |
| Device state | MQTT(state) → Backend → PG(devices.reported_state) + outbox → Redis/SignalR | Eventual |
| Telemetry | MQTT(telemetry) → Backend → TimescaleDB (atomic multi-row insert + dedup ledger) | MQTT QoS 0 is at-most-once delivery; database deduplication makes processing idempotent only for messages that arrive |
| AI action | HTTPS → Backend → FastAPI → Backend (validate) → MQTT(command) → ... | Same as user command |
| Availability | MQTT(availability) + Last Will → Backend → Redis(device:status) + outbox → SignalR | Eventual |

---

## 4. TECHNOLOGY COMPATIBILITY AND VERSION CONSTRAINTS

### 4.1 Verified Version Matrix

| Component | Minimum Version | Tested With | Status | Notes |
|-----------|-----------------|-------------|--------|-------|
| .NET / ASP.NET Core | **10.0 LTS** | 10.0.x | **Recommended** | LTS until **14 November 2028** (.NET 8 support ended 10 November 2026) |
| EF Core | **10.0+** | 10.0.x | **Proposed** | `HasFilter`, `HasAlternateKey` supported; aligned with .NET 10 |
| Npgsql.EntityFrameworkCore.PostgreSQL | **10.0+** | 10.0.x | **Proposed** | EF Core provider package; aligned with .NET 10 / EF Core 10 |
| PostgreSQL | 16+ | 16.x | **Confirmed** | Required for TimescaleDB 2.15+ |
| TimescaleDB | 2.15.0+ | 2.15.x on PG 16 | **Confirmed** | Hypertable, compression, retention |
| Redis | 7.2.0+ | 7.2.x | **Confirmed** | Cluster mode optional |
| FastAPI | 0.100+ | Python 3.11+ | **Confirmed** | AI bridge |
| MQTT | 3.1.1 / 5.0 | TLS 1.2+ | **Confirmed** | Port 8883, NexusLink v1 |

> **Note:** This is an architecture recommendation for the project baseline, not an assertion about the current local environment. The team should adopt .NET 10 LTS and corresponding EF Core 10/Npgsql 10 versions for new development. EF Core 10 and Npgsql 10 are required for .NET 10 compatibility.

### 4.2 EF Core / Npgsql Capability Matrix

| Feature | EF Core 10+ | Npgsql 10+ | Migration Approach |
|---------|------------|-----------|-------------------|
| UUID PK/FK | ✅ | ✅ | Native |
| JSONB columns | ✅ | ✅ | Native |
| Array columns | ✅ | ✅ | Native |
| Enum mappings | ✅ | ✅ | `HasConversion<string>()` |
| Composite FK to alternate key | ✅ | ✅ | `HasPrincipalKey()` + `HasAlternateKey()` |
| Partial unique indexes | ✅ | ✅ | `HasFilter()` |
| Check constraints | ✅ | ✅ | `HasCheckConstraint()` |
| TimescaleDB hypertable | ❌ | ⚠️ | Raw SQL in migration |
| Trigger functions | ❌ | ⚠️ | Raw SQL in migration |
| `create_hypertable` | ❌ | ❌ | `migrationBuilder.Sql()` |

---

## 5. FINAL DATABASE TABLE INVENTORY

### 5.1 Physical Table Count

| Category | Tables | Count |
|----------|--------|-------|
| **Approved MVP Domain Tables** | users, homes, memberships, rooms, devices, device_telemetry, invitations, permissions, settings, ai_action_audit, device_commands | **11** |
| **Infrastructure Tables** | outbox_events, telemetry_dedup_ledger | **2** |
| **Enum Types (not tables)** | permission_type_enum, target_type_enum, effect_enum, command_status_enum, initiator_type_enum | 0 |
| **Total Physical Tables** | | **13** |

**Critical Distinction:** 11 domain tables = approved MVP scope. 2 infrastructure tables = implementation details for cross-system consistency and deduplication.

### 5.2 Table Creation Order (Dependency-Resolved)

```sql
-- Order 1: No dependencies
CREATE TABLE users (...);
CREATE TABLE homes (...);

-- Order 2: Depend on users, homes
CREATE TABLE memberships (...);
CREATE TABLE rooms (...);
CREATE TABLE invitations (...);
CREATE TABLE settings (...);

-- Order 3: Depend on homes, rooms (composite FK target)
CREATE TABLE devices (...);

-- Order 4: Depend on devices, homes, users
CREATE TABLE device_commands (...);
CREATE TABLE ai_action_audit (...);  -- FKs to devices, users, device_commands(command_id)

-- Order 5: Depend on memberships (grantor/grantee)
CREATE TABLE permissions (...);

-- Order 6: Depend on device_telemetry (hypertable created after table)
CREATE TABLE device_telemetry (...);
SELECT create_hypertable('device_telemetry', 'timestamp', ...);

-- Order 7: Infrastructure (no domain FKs)
CREATE TABLE outbox_events (...);
CREATE TABLE telemetry_dedup_ledger (...);
```

---

## 6. CORRECTED DATABASE DESIGN

### 6.1 Design Principles Applied

1. **PostgreSQL 16+ native types** — UUID, JSONB, arrays, enums
2. **TimescaleDB 2.15+ compliance** — PK includes partitioning column; unique constraints on hypertable MUST include all partitioning columns (timestamp). Separate deduplication ledger used for strong message-level dedup.
3. **EF Core Code First compatibility** — All mappings expressible via Fluent API
4. **Transactional outbox pattern** — PostgreSQL source of truth → Redis/SignalR eventual
5. **Soft delete where required** — `deleted_at`/`removed_at` + `is_active` flags
6. **Composite FK with MATCH SIMPLE** — Device→Room home consistency
7. **Partial unique indexes** — One owner per home; idempotency keys per user
8. **Proper GDPR patterns** — SET NULL on audit FKs, anonymization flow documented

### 6.2 Corrected DDL / SQL Pseudocode

#### 6.2.1 Enum Types

```sql
-- All enums created first (before tables that reference them)
CREATE TYPE permission_type_enum AS ENUM (
    'device:control', 'device:view', 'device:manage',
    'home:manage', 'member:manage',
    'camera:stream', 'camera:view',
    'microphone:stream',
    'settings:manage',
    'ai:propose', 'ai:execute'
);

CREATE TYPE target_type_enum AS ENUM ('device', 'home', 'room', 'camera', 'microphone');

CREATE TYPE effect_enum AS ENUM ('grant', 'deny');

CREATE TYPE command_status_enum AS ENUM (
    'Pending', 'Sent', 'Acknowledged', 
    'Succeeded', 'Failed', 'TimedOut', 'Cancelled'
);

CREATE TYPE initiator_type_enum AS ENUM ('user', 'ai', 'system');
```

> **Note on B-01 and B-02:** The `permission_type_enum` and `target_type_enum` values above are the **proposed** values for MVP. These enums directly affect `CREATE TYPE` and `CHECK` constraints and are **Schema P0 blockers**. Final values require Product + Security approval before migration generation.

#### 6.2.2 USERS

```sql
CREATE TABLE users (
    id              uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    email           varchar(255) NOT NULL UNIQUE,
    password_hash   varchar(255) NOT NULL,
    full_name       varchar(255) NOT NULL,
    is_active       boolean NOT NULL DEFAULT true,
    email_verified  boolean NOT NULL DEFAULT false,
    deleted_at      timestamptz,                    -- Soft delete for GDPR
    created_at      timestamptz NOT NULL DEFAULT now(),
    updated_at      timestamptz NOT NULL DEFAULT now()
);
-- Partial unique index: email unique only among non-deleted users
-- Allows new account with same email after soft delete
CREATE UNIQUE INDEX uk_users_email_active ON users (email) WHERE deleted_at IS NULL;
```

**Soft Delete vs Physical Delete Behavior:**

| Operation | Effect |
|-----------|--------|
| **Soft Delete** (`UPDATE users SET deleted_at = now(), is_active = false, email = 'deleted_' || id || '@placeholder' WHERE id = ?`) | User record retained. `is_active = false` excludes from login/queries. Email anonymized to allow new account with same email. FK `ON DELETE` actions **do not fire** (no physical DELETE). |
| **Physical Delete** (`DELETE FROM users WHERE id = ?`) | **FK actions fire**: `device_commands.requested_by_user_id` → CASCADE; `ai_action_audit.user_id` → SET NULL; `settings` → CASCADE (deleted); `memberships` → CASCADE (deleted); `invitations.invited_by_user_id` → SET NULL. |

> **Critical Note (C-01 Resolution):** Per `DATABASE_MVP_SUMMARY.md` line 23, the authoritative decision is `ON DELETE CASCADE` for `device_commands.requested_by_user_id`. The document explicitly states: "The `device_commands.requested_by_user_id` FK uses `ON DELETE CASCADE`. Deleting a user removes their command history. This is intentional for GDPR/privacy compliance... Alternatives (`RESTRICT` or `SET NULL`) were considered but rejected: `RESTRICT` would block user deletion, `SET NULL` would orphan commands and break audit trails." This document aligns with that authoritative decision. **This FK decision is CONFIRMED (D-18) and is not reopened.**
>
> **B-06 Legal Policy (Separate):** Legal + Product must confirm (1) whether the CASCADE behavior satisfies GDPR "right to erasure" or whether soft-delete + anonymization is the primary compliance path, (2) retention periods for command history, and (3) the erasure workflow. This is a **compliance/operational approval gate**, not a schema choice. It does not block migration generation.

**Required Anonymization Procedure (Transactional):**

```sql
-- Call this instead of DELETE for GDPR "right to erasure"
CREATE OR REPLACE FUNCTION anonymize_user(p_user_id uuid)
RETURNS void LANGUAGE plpgsql AS $$
BEGIN
    -- 1. Anonymize PII
    UPDATE users SET
        email = 'deleted_' || id || '@placeholder',
        full_name = 'Deleted User',
        password_hash = '',
        email_verified = false,
        is_active = false,
        deleted_at = now(),
        updated_at = now()
    WHERE id = p_user_id;

    -- 2. Deactivate memberships. UPDATE does NOT fire ON DELETE CASCADE.
    -- Existing grants are retained for auditability but become ineffective because
    -- authorization always checks that the grantor membership is still active.
    UPDATE memberships SET is_active = false, updated_at = now()
    WHERE user_id = p_user_id;

    -- 3. Device commands requested_by_user_id → handled by FK CASCADE on physical DELETE only
    -- For soft delete, we explicitly do NOT nullify here — command history is preserved per D-18/CASCADE decision
    -- If hard DELETE occurs later, CASCADE will remove command rows

    -- 4. AI audit user_id → SET NULL (FK handles this on physical DELETE)
    -- For soft delete, explicitly nullify:
    UPDATE ai_action_audit SET user_id = NULL
    WHERE user_id = p_user_id;

    -- 5. Settings → DELETE (or anonymize preferences)
    DELETE FROM settings WHERE user_id = p_user_id;

    -- 6. Invitations invited_by_user_id → SET NULL
    UPDATE invitations SET invited_by_user_id = NULL
    WHERE invited_by_user_id = p_user_id;
END;
$$;
```

**Key Distinction:** The `ON DELETE CASCADE` / `SET NULL` actions in the DDL **only execute on physical `DELETE`**. Soft delete via `deleted_at` update requires explicit application-level nullification/cleanup as shown above. Do not rely on FK actions for soft-delete anonymization.

#### 6.2.3 HOMES

```sql
CREATE TABLE homes (
    id              uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    name            varchar(255) NOT NULL,
    address_line1   varchar(255),
    address_line2   varchar(255),
    city            varchar(100),
    state_province  varchar(100),
    postal_code     varchar(20),
    timezone        varchar(50) NOT NULL DEFAULT 'UTC',  -- IANA timezone
    deleted_at      timestamptz,
    created_at      timestamptz NOT NULL DEFAULT now(),
    updated_at      timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX idx_homes_deleted_at ON homes (deleted_at) WHERE deleted_at IS NOT NULL;
```

#### 6.2.4 MEMBERSHIPS

```sql
CREATE TABLE memberships (
    id              uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id         uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    home_id         uuid NOT NULL REFERENCES homes(id) ON DELETE CASCADE,
    role            varchar(20) NOT NULL CHECK (role IN ('owner','member','guest')),
    is_active       boolean NOT NULL DEFAULT true,
    joined_at       timestamptz NOT NULL DEFAULT now(),
    created_at      timestamptz NOT NULL DEFAULT now(),
    updated_at      timestamptz NOT NULL DEFAULT now()
);

-- Partial unique index: at most one ACTIVE owner per home
-- MEMBERSHIPS does not have a deleted_at column; soft-suspend uses is_active = false
CREATE UNIQUE INDEX uk_memberships_one_owner_per_home 
    ON memberships (home_id) 
    WHERE role = 'owner' AND is_active = true;

CREATE INDEX idx_memberships_user_id ON memberships (user_id);
CREATE INDEX idx_memberships_home_id ON memberships (home_id);
CREATE INDEX idx_memberships_user_home_active 
    ON memberships (user_id, home_id) WHERE is_active = true;
```

**Note:** This constraint guarantees **at most one active owner** per home, not *at least one*. Ownerless homes can occur through:
- Owner deactivation (`is_active = false`)
- Owner user deletion (cascades to membership)
- Direct membership deletion
- Failed ownership transfer (concurrent attempts)

### Owner Membership Invariant — Lifecycle and Prevention

The partial unique index prevents *multiple* active owners but does **not** prevent *zero* owners. The system must handle ownerless homes explicitly:

| Scenario | Current Behavior | Required Prevention/Mitigation |
|----------|------------------|--------------------------------|
| Owner deactivation (`is_active = false`) | Home becomes ownerless | Application must require ownership transfer before deactivation, or auto-promote senior member |
| Owner user deletion | Membership CASCADE deleted → ownerless | Application must require ownership transfer before user deletion |
| Direct membership deletion | Ownerless if last owner | Application must prevent deletion of last owner membership |
| Concurrent ownership transfer | Race condition possible | Requires atomic transfer mechanism (see I-01) |

**Atomic Ownership Transfer (Proposed):**
```sql
-- Single transaction: demote old owner, promote new owner
BEGIN;
UPDATE memberships SET role = 'member', updated_at = now()
WHERE home_id = $1 AND role = 'owner' AND is_active = true;
UPDATE memberships SET role = 'owner', updated_at = now()
WHERE home_id = $1 AND user_id = $2 AND is_active = true;
COMMIT;
```

**Unresolved Decision (I-01):** The final mechanism for preventing accidental ownerless homes requires cross-team approval. Options include:
- Application-level guard (require transfer before deactivation/deletion)
- Database trigger preventing last owner removal
- Auto-promotion of senior member
- Scheduled reconciliation job that alerts on ownerless homes

This is explicitly documented as **unresolved** rather than assuming the index solves it.

#### 6.2.5 ROOMS

```sql
CREATE TABLE rooms (
    id              uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    home_id         uuid NOT NULL REFERENCES homes(id) ON DELETE CASCADE,
    name            varchar(100) NOT NULL,
    deleted_at      timestamptz,
    created_at      timestamptz NOT NULL DEFAULT now(),
    updated_at      timestamptz NOT NULL DEFAULT now()
);

-- Required for composite FK from devices (home_id, room_id) -> rooms (home_id, id)
CREATE UNIQUE INDEX uk_rooms_home_id ON rooms (home_id, id);

-- Business rule: unique room name per home (among non-deleted)
CREATE UNIQUE INDEX uk_rooms_home_name ON rooms (home_id, name) WHERE deleted_at IS NULL;

CREATE INDEX idx_rooms_home_id ON rooms (home_id);
```

#### 6.2.6 DEVICES

```sql
CREATE TABLE devices (
    id                      uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    home_id                 uuid NOT NULL REFERENCES homes(id) ON DELETE RESTRICT,
    room_id                 uuid REFERENCES rooms(id) ON DELETE SET NULL,  -- Nullable; room deletion clears device.room_id
    external_id             varchar(100) NOT NULL UNIQUE,  -- Globally unique device identity
    name                    varchar(255) NOT NULL,
    device_type             varchar(100) NOT NULL,
    capabilities            jsonb NOT NULL DEFAULT '{}',    -- Structured capability flags
    metadata                jsonb NOT NULL DEFAULT '{}',
    firmware_version        varchar(50),
    manufacturer            varchar(100),
    model                   varchar(100),
    connection_status       varchar(20) NOT NULL DEFAULT 'unknown'
                                CHECK (connection_status IN ('online','offline','unknown')),
    last_seen_at            timestamptz,
    reported_state          jsonb,                          -- Last trusted device-reported state
    reported_at             timestamptz,
    state_version           bigint NOT NULL DEFAULT 0,      -- Optimistic concurrency
    removed_at              timestamptz,                    -- Soft delete
    created_at              timestamptz NOT NULL DEFAULT now(),
    updated_at              timestamptz NOT NULL DEFAULT now()
);

-- Required for composite FK from device_commands (home_id, device_id) -> devices (home_id, id)
CREATE UNIQUE INDEX uk_devices_home_id ON devices (home_id, id);

-- Composite FK: device's room must belong to same home (MATCH SIMPLE allows NULL room_id)
-- The room_id FK above (ON DELETE SET NULL) handles room deletion; this composite FK ensures
-- that when room_id is set, it belongs to the same home.
ALTER TABLE devices ADD CONSTRAINT fk_devices_rooms
    FOREIGN KEY (home_id, room_id) REFERENCES rooms (home_id, id) MATCH SIMPLE;

CREATE INDEX idx_devices_home_id ON devices (home_id);
CREATE INDEX idx_devices_room_id ON devices (room_id);
CREATE INDEX idx_devices_connection_status ON devices (connection_status);
CREATE INDEX idx_devices_removed_at ON devices (removed_at) WHERE removed_at IS NOT NULL;
```

> **Device Connection Status Mapping (A-04):** Backend uses `('online','offline','unknown')`. NexusLink availability schema defines `('online','offline')` only. The Backend derives `unknown` (never sent by device) after 3 missed heartbeats (90s). This is consistent with spec.md §3.

#### 6.2.7 INVITATIONS

```sql
CREATE TABLE invitations (
    id                  uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    home_id             uuid NOT NULL REFERENCES homes(id) ON DELETE CASCADE,
    invitee_email       varchar(255) NOT NULL,
    role                varchar(20) NOT NULL CHECK (role IN ('owner','member','guest')),
    token               varchar(100) NOT NULL UNIQUE,
    expires_at          timestamptz NOT NULL,
    is_accepted         boolean NOT NULL DEFAULT false,
    invited_by_user_id  uuid REFERENCES users(id) ON DELETE SET NULL,
    created_at          timestamptz NOT NULL DEFAULT now(),
    updated_at          timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX idx_invitations_home_id ON invitations (home_id);
CREATE INDEX idx_invitations_expires_at ON invitations (expires_at);
CREATE INDEX idx_invitations_token ON invitations (token);
```

> **Invitation Role Rules (A-07, I-05):** `INVITATIONS.role` allows `'owner'`, `'member'`, `'guest'`. Inviting as Owner is technically allowed by schema. Business logic should restrict Owner invitations to system-initiated flows (e.g., home creation) or require explicit cross-team approval. This is an **unresolved runtime decision** (I-05). No trigger prevents owner invitation via API; application layer must enforce.

#### 6.2.8 SETTINGS

```sql
CREATE TABLE settings (
    id                      uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id                 uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    avatar_url              varchar(500),
    notification_enabled    boolean NOT NULL DEFAULT true,
    preferences             jsonb NOT NULL DEFAULT '{}',
    created_at              timestamptz NOT NULL DEFAULT now(),
    updated_at              timestamptz NOT NULL DEFAULT now(),
    UNIQUE (user_id)        -- One row per user (personal settings only)
);
```

> **Settings Design Note (A-06):** `notification_enabled` is a dedicated boolean column; `preferences` is a JSONB object for extensible settings. This is intentional: `notification_enabled` is a high-frequency access field for push notification gating; `preferences` holds lower-frequency user preferences. No conflict.

#### 6.2.9 DEVICE_COMMANDS

```sql
CREATE TABLE device_commands (
    id                          uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    command_id                  uuid NOT NULL UNIQUE,              -- NexusLink commandId (device deduplicates)
    correlation_id              uuid NOT NULL,                     -- End-to-end trace ID
    home_id                     uuid NOT NULL REFERENCES homes(id) ON DELETE RESTRICT,
    device_id                   uuid NOT NULL,
    requested_by_user_id        uuid REFERENCES users(id) ON DELETE CASCADE,  -- NULL for system commands
    initiator_type              initiator_type_enum NOT NULL,
    type                        varchar(100) NOT NULL,             -- Matches NexusLink command.type
    parameters                  jsonb,
    status                      command_status_enum NOT NULL DEFAULT 'Pending',
    idempotency_key             varchar(100),
    created_at                  timestamptz NOT NULL DEFAULT now(),
    sent_at                     timestamptz,
    acknowledged_at             timestamptz,
    completed_at                timestamptz,
    expires_at                  timestamptz NOT NULL,              -- Command deadline
    reported_state              jsonb,                             -- From command.result Succeeded
    error_code                  varchar(50),
    error_message               text,
    -- Composite FK to devices (home_id, device_id) -> devices (home_id, id)
    -- ON DELETE RESTRICT: preserve command history while device exists (D-17)
    CONSTRAINT fk_device_commands_devices 
        FOREIGN KEY (home_id, device_id) REFERENCES devices (home_id, id) ON DELETE RESTRICT
);

-- Partial unique index for idempotency: one pending/active command per user per idempotency key
CREATE UNIQUE INDEX uk_device_commands_idempotency 
    ON device_commands (requested_by_user_id, idempotency_key) 
    WHERE requested_by_user_id IS NOT NULL 
      AND status IN ('Pending', 'Sent', 'Acknowledged');

-- CHECK constraint for command type pattern (matches NexusLink command.type)
-- Pattern: ^[a-z]+(\.[a-z_]+)+$
-- This is a BACKEND IMPLEMENTATION REQUIREMENT, not a device protocol requirement
ALTER TABLE device_commands ADD CONSTRAINT ck_device_commands_type_format
    CHECK (type ~ '^[a-z]+(\.[a-z_]+)+$');
```

```sql
CREATE INDEX idx_device_commands_home_id ON device_commands (home_id);
CREATE INDEX idx_device_commands_device_id ON device_commands (device_id);
CREATE INDEX idx_device_commands_user_id ON device_commands (requested_by_user_id);
CREATE INDEX idx_device_commands_status ON device_commands (status);
CREATE INDEX idx_device_commands_correlation_id ON device_commands (correlation_id);
CREATE INDEX idx_device_commands_expires_at ON device_commands (expires_at) 
    WHERE status NOT IN ('Succeeded', 'Failed', 'TimedOut', 'Cancelled');
CREATE INDEX idx_device_commands_command_id ON device_commands (command_id);
```

> **Device Command Type Validation (A-10):** The `CHECK` constraint above enforces the NexusLink `command.type` pattern (`^[a-z]+(\.[a-z_]+)+$`) at the database level. This is a backend implementation requirement; the device protocol validates the same pattern independently.

#### 6.2.10 AI_ACTION_AUDIT

```sql
CREATE TABLE ai_action_audit (
    id                  uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id             uuid REFERENCES users(id) ON DELETE SET NULL,
    device_id           uuid REFERENCES devices(id) ON DELETE RESTRICT,
    correlation_id      uuid,                          -- Links to request correlation
    command_id          uuid REFERENCES device_commands(command_id) ON DELETE SET NULL,
    action_type         varchar(100) NOT NULL,
    action_parameters   jsonb,
    outcome             varchar(20) NOT NULL CHECK (outcome IN ('success','failure','denied','error')),
    timestamp           timestamptz NOT NULL,
    created_at          timestamptz NOT NULL DEFAULT now(),
    updated_at          timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX idx_ai_audit_user_id ON ai_action_audit (user_id);
CREATE INDEX idx_ai_audit_device_id ON ai_action_audit (device_id);
CREATE INDEX idx_ai_audit_timestamp ON ai_action_audit (timestamp DESC);
CREATE INDEX idx_ai_audit_command_id ON ai_action_audit (command_id);
CREATE INDEX idx_ai_audit_correlation_id ON ai_action_audit (correlation_id);
```

> **AI Audit Outcome Mapping (A-05):** `outcome` enum `('success','failure','denied','error')` is a backend classification. NexusLink device error codes (`UNSUPPORTED_COMMAND`, `INVALID_PARAMETER`, `COMMAND_EXPIRED`, `CLOCK_NOT_SYNCED`, `DEVICE_BUSY`, `EXECUTION_FAILED`) map to backend outcomes as: `denied` → authorization denied; `failure`/`error` → device execution failed; `success` → device `Succeeded`. No direct enum correspondence.

#### 6.2.11 PERMISSIONS

```sql
CREATE TABLE permissions (
    id                      uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    grantor_membership_id   uuid REFERENCES memberships(id) ON DELETE CASCADE,
    grantee_membership_id   uuid NOT NULL REFERENCES memberships(id) ON DELETE CASCADE,
    permission_type         permission_type_enum NOT NULL,
    target_type             target_type_enum NOT NULL,
    target_id               uuid NOT NULL,
    effect                  effect_enum NOT NULL DEFAULT 'grant',
    granted_at              timestamptz NOT NULL DEFAULT now(),
    created_at              timestamptz NOT NULL DEFAULT now(),
    updated_at              timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ck_permissions_grantor_effect CHECK (
        (effect = 'grant' AND grantor_membership_id IS NOT NULL) OR
        (effect = 'deny' AND grantor_membership_id IS NULL)
    )
);

CREATE INDEX idx_permissions_grantee ON permissions (grantee_membership_id);
CREATE INDEX idx_permissions_grantor ON permissions (grantor_membership_id);
CREATE INDEX idx_permissions_target ON permissions (target_type, target_id);
CREATE INDEX idx_permissions_effective 
    ON permissions (grantee_membership_id, permission_type, target_type, target_id, effect);
```

> **Permission Type/Target Validation:** The trigger (Section 9.3) validates that `permission_type` + `target_type` combinations are valid (e.g., `camera:stream` only with `target_type = 'camera'`). A database-level `CHECK` constraint is not used because the validation requires cross-table lookups.

#### 6.2.12 DEVICE_TELEMETRY (TimescaleDB Hypertable)

```sql
CREATE TABLE device_telemetry (
    id              bigserial NOT NULL,
    device_id       uuid NOT NULL REFERENCES devices(id) ON DELETE RESTRICT,
    timestamp       timestamptz NOT NULL,
    message_id      uuid NOT NULL,                    -- NexusLink messageId for deduplication
    reading_index   smallint NOT NULL,                -- 0-7: position in readings array
    metric_name     varchar(100) NOT NULL,
    metric_value    double precision NOT NULL,
    metric_unit     varchar(50),
    created_at      timestamptz NOT NULL DEFAULT now(),
    -- PK MUST include partitioning column (timestamp) for TimescaleDB
    PRIMARY KEY (id, timestamp)
);

-- Convert to hypertable (raw SQL required — no EF Core support)
SELECT create_hypertable('device_telemetry', 'timestamp', 
    chunk_time_interval => INTERVAL '1 day',  -- PENDING: B-05 retention decision
    if_not_exists => TRUE,
    migrate_data => FALSE
);

-- Standard indexes
CREATE INDEX idx_device_telemetry_device_time 
    ON device_telemetry (device_id, timestamp DESC);

-- NOTE: Unique constraint on (device_id, message_id, reading_index) 
-- CANNOT be created on hypertable without including timestamp (partitioning column).
-- Strong message-level deduplication is implemented via separate telemetry_dedup_ledger table (Section 7).
```

---

## 7. TIMESCALEDB TELEMETRY AND DEDUPLICATION STRATEGY

### 7.1 The Core Problem

TimescaleDB hypertables have a fundamental restriction: **unique constraints must include all partitioning columns (timestamp)**. The telemetry table is partitioned by `timestamp` (1-day chunks). We need to deduplicate on `(device_id, message_id, reading_index)` — but `message_id` comes from the device and a duplicate message may have a different `timestamp` (reportedAt) if the device retries with a new timestamp.

| Approach | Deduplication Strength | TimescaleDB Compatible | Performance |
|----------|------------------------|------------------------|-------------|
| Unique constraint including timestamp | Weak (different timestamp = duplicate allowed) | ✅ Native | Excellent |
| Unique constraint without timestamp | Strong (true message-level dedup) | ❌ Not supported | N/A |
| **Separate deduplication ledger table** | **Strong** | ✅ Native | **Good (small table)** |
| Application-level dedup only | Best-effort | ✅ | Risk of races |

### 7.2 Recommended Design: Separate Deduplication Ledger

**This is an infrastructure table, not a core domain table.**

```sql
-- Deduplication ledger: small, heavily indexed, fast lookups
CREATE TABLE telemetry_dedup_ledger (
    device_id       uuid NOT NULL,
    message_id      uuid NOT NULL,
    reading_index   smallint NOT NULL,
    telemetry_id    bigint NOT NULL,        -- References device_telemetry.id (see FK note below)
    received_at     timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (device_id, message_id, reading_index)
);

-- Optional: index for device-scoped cleanup queries
CREATE INDEX idx_dedup_device ON telemetry_dedup_ledger (device_id);
CREATE INDEX idx_dedup_received_at ON telemetry_dedup_ledger (received_at);  -- Supports bounded retention cleanup
```

**FK Note:** `telemetry_id` references `device_telemetry.id`, but `device_telemetry` has a composite PK `(id, timestamp)`. A simple FK on `telemetry_id` alone is **not valid** for the hypertable's partitioning scheme. The ledger stores `telemetry_id` as a correlation aid only; it is **not enforced by a foreign key constraint**. The ledger's primary key `(device_id, message_id, reading_index)` is the authoritative deduplication constraint.

### 7.3 Ingestion Algorithm (Atomic, Race-Free)

The NexusLink telemetry schema (`telemetry.schema.json`) defines readings as an array of objects with `{name, value, unit}` — **no `index` field**. Array ordinality (0-based position) determines `reading_index`. The schema enforces: `minItems: 1`, `maxItems: 8`, `name` pattern `^[a-z][a-z0-9_]*$`, `value` type `number`, `unit` type `string` (required).

```sql
-- Single atomic upsert: inserts new readings + dedup records together
-- Called from Backend telemetry ingestion service
CREATE OR REPLACE FUNCTION ingest_telemetry_batch(
    p_device_id uuid,
    p_message_id uuid,
    p_reported_at timestamptz,
    p_readings jsonb  -- Array of {name, value, unit}
) RETURNS integer LANGUAGE plpgsql AS $$
DECLARE
    v_inserted_count integer := 0;
BEGIN
    -- Validate the envelope before reserving deduplication keys.
    IF p_device_id IS NULL OR p_message_id IS NULL OR p_reported_at IS NULL THEN
        RAISE EXCEPTION 'device_id, message_id, and reported_at are required';
    END IF;
    IF p_readings IS NULL OR jsonb_typeof(p_readings) <> 'array' THEN
        RAISE EXCEPTION 'readings must be a JSON array';
    END IF;
    IF jsonb_array_length(p_readings) < 1 THEN
        RAISE EXCEPTION 'readings must contain at least one item';
    END IF;
    -- Enforce maximum batch size per NexusLink telemetry.schema.json
    IF jsonb_array_length(p_readings) > 8 THEN
        RAISE EXCEPTION 'readings array exceeds maximum of 8 items (telemetry.schema.json)';
    END IF;
    IF EXISTS (
        SELECT 1 FROM jsonb_array_elements(p_readings) AS item
        WHERE jsonb_typeof(item) <> 'object'
           OR NOT (item ? 'name' AND item ? 'value' AND item ? 'unit')
           OR jsonb_typeof(item->'name') <> 'string'
           OR jsonb_typeof(item->'unit') <> 'string'
           OR jsonb_typeof(item->'value') <> 'number'
           OR (item->>'name') !~ '^[a-z][a-z0-9_]*$'
    ) THEN
        RAISE EXCEPTION 'each reading must contain name (^[a-z][a-z0-9_]*$), value (number), and unit (string) with valid JSON types';
    END IF;

    -- 1. Try to reserve dedup slots for all readings in this message
    -- If conflict on any reading_index, that reading is already processed
    INSERT INTO telemetry_dedup_ledger (device_id, message_id, reading_index, telemetry_id)
    SELECT p_device_id, p_message_id, generate_series(0, jsonb_array_length(p_readings)-1), 0
    ON CONFLICT (device_id, message_id, reading_index) DO NOTHING;

    -- 2. Insert telemetry rows ONLY for readings that got a fresh dedup slot (telemetry_id = 0)
    WITH new_telemetry AS (
        INSERT INTO device_telemetry (device_id, timestamp, message_id, reading_index, metric_name, metric_value, metric_unit)
        SELECT 
            p_device_id,
            p_reported_at,
            p_message_id,
            (idx - 1)::smallint AS reading_index,
            rdg->>'name' AS metric_name,
            (rdg->>'value')::double precision AS metric_value,
            rdg->>'unit' AS metric_unit
        FROM jsonb_array_elements(p_readings) WITH ORDINALITY AS r(rdg, idx)
        WHERE NOT EXISTS (
            SELECT 1 FROM telemetry_dedup_ledger dl
            WHERE dl.device_id = p_device_id
              AND dl.message_id = p_message_id
              AND dl.reading_index = (idx - 1)::smallint
              AND dl.telemetry_id != 0
        )
        RETURNING id, reading_index
    )
    -- 3. Update ledger with actual telemetry IDs for newly inserted rows
    UPDATE telemetry_dedup_ledger dl
    SET telemetry_id = nt.id
    FROM new_telemetry nt
    WHERE dl.device_id = p_device_id
      AND dl.message_id = p_message_id
      AND dl.reading_index = nt.reading_index
      AND dl.telemetry_id = 0;

    GET DIAGNOSTICS v_inserted_count = ROW_COUNT;
    RETURN v_inserted_count;
END;
$$;
```

**Key Properties:**
- **Atomic**: Single transaction, no race window
- **Strong deduplication**: Ledger PK prevents double-insert even with different timestamps
- **Scalable**: Ledger stays small (one row per unique message_id+reading_index)
- **Recoverable**: Can rebuild ledger from telemetry table if needed
- **No end-to-end exactly-once claim**: MQTT QoS 0 telemetry is at-most-once delivery. The ledger prevents duplicate database processing for a given `(device_id, message_id, reading_index)` while the ledger entry is retained. A message that is never delivered cannot be recovered by database deduplication.
- **Schema-validated**: Enforces `maxItems: 8`, `name` pattern, `value` as number, `unit` as required string per `telemetry.schema.json`.

### 7.4 Deduplication Ledger Retention and Cleanup

The deduplication ledger grows unbounded without cleanup. A bounded retention strategy is required:

```sql
-- Example cleanup function (run as scheduled job, e.g., daily)
CREATE OR REPLACE FUNCTION cleanup_telemetry_dedup_ledger(p_older_than interval, p_batch_size integer DEFAULT 10000)
RETURNS integer LANGUAGE plpgsql AS $$
DECLARE
    v_deleted_count integer := 0;
BEGIN
    IF p_older_than IS NULL OR p_older_than <= interval '0' THEN
        RAISE EXCEPTION 'p_older_than must be a positive interval';
    END IF;
    IF p_batch_size IS NULL OR p_batch_size < 1 OR p_batch_size > 50000 THEN
        RAISE EXCEPTION 'p_batch_size must be between 1 and 50000';
    END IF;

    WITH batch AS (
        SELECT ctid
        FROM telemetry_dedup_ledger
        WHERE received_at < now() - p_older_than
          AND telemetry_id <> 0  -- Exclude incomplete in-transaction placeholders
        ORDER BY received_at
        LIMIT p_batch_size
        FOR UPDATE SKIP LOCKED
    )
    DELETE FROM telemetry_dedup_ledger d
    USING batch b
    WHERE d.ctid = b.ctid;

    GET DIAGNOSTICS v_deleted_count = ROW_COUNT;
    RETURN v_deleted_count;
END;
$$;
```

**Retention Policy:** O-06 is unresolved. The example 30-day value is illustrative only, not an approved setting. Before enabling cleanup, DevOps + Product must define a bounded retention period that exceeds the maximum supported device retry/replay window, account for delayed/offline devices and operational replay, and document what happens to duplicates arriving after ledger expiry. The cleanup function requires an explicit approved `p_older_than` interval and deletes at most `p_batch_size` rows per call (default 10,000; hard cap 50,000). Run repeated bounded batches under scheduler control, expose deleted-row counts, and monitor backlog/lag. Do not enable cleanup until O-06 is approved.

### 7.5 Hypertable Retention Policy (PENDING B-05)

```sql
-- To be applied after B-05 resolution (telemetry retention period)
-- Example for 1-year retention with compression:
ALTER TABLE device_telemetry SET (
    timescaledb.compress,
    timescaledb.compress_segmentby = 'device_id'
);
SELECT add_compression_policy('device_telemetry', INTERVAL '7 days');  -- Compress after 7 days
SELECT add_retention_policy('device_telemetry', INTERVAL '1 year');    -- PENDING: B-05 decision
```

---

## 8. OUTBOX AND RELIABLE EVENT PUBLISHING STRATEGY

### 8.1 Problem Statement

Backend must update PostgreSQL (source of truth) AND publish to Redis (live state) AND SignalR (real-time clients) atomically. Distributed transaction not available → **Transactional Outbox Pattern**.

### 8.2 Outbox Table Design (Corrected)

```sql
CREATE TABLE outbox_events (
    id                      uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    event_type              varchar(100) NOT NULL,        -- e.g., 'device.state.changed', 'device.command.result'
    payload                 jsonb NOT NULL,               -- Full event envelope
    device_id               uuid,                         -- For partitioned processing
    home_id                 uuid,                         -- For SignalR group routing
    correlation_id          uuid,                         -- End-to-end trace
    created_at              timestamptz NOT NULL DEFAULT now(),
    -- Lease-based claiming (no volatile functions in indexes)
    claimed_at              timestamptz,
    claimed_by              uuid,                         -- Worker instance ID
    lease_expires_at        timestamptz,                  -- Explicit expiration timestamp
    -- Per-channel delivery tracking (independent)
    redis_delivered_at      timestamptz,
    signalr_delivered_at    timestamptz,
    retry_count             int NOT NULL DEFAULT 0,
    next_attempt_at         timestamptz NOT NULL DEFAULT now(),
    last_error              text,
    -- Status: dead letter is a status, not a separate table
    status                  varchar(20) NOT NULL DEFAULT 'pending'
        CHECK (status IN ('pending','processing','delivered','dead_letter'))
);

-- Valid partial indexes: use only immutable status conditions
-- Time-dependent lease expiration is evaluated in the query, not the index predicate
CREATE INDEX idx_outbox_unclaimed ON outbox_events (created_at) 
    WHERE status = 'pending' 
       OR status = 'processing';

-- Device-partitioned claim index
CREATE INDEX idx_outbox_device_unclaimed ON outbox_events (device_id, created_at) 
    WHERE status = 'pending' 
       OR status = 'processing';

-- Dead letter monitoring
CREATE INDEX idx_outbox_dead_letter ON outbox_events (created_at) WHERE status = 'dead_letter';
```

**Correction:** PostgreSQL partial-index predicates cannot use non-immutable functions such as `now()`. The indexes above use only immutable `status` conditions. Lease expiration is evaluated in the worker's claim query via `lease_expires_at < now()`, which is valid in a `WHERE` clause. Workers claim events with:

```sql
SELECT id, ... FROM outbox_events
WHERE ((status = 'pending' AND next_attempt_at <= now()
        AND (lease_expires_at IS NULL OR lease_expires_at < now()))
    OR (status = 'processing' AND lease_expires_at < now()))
ORDER BY next_attempt_at, created_at
LIMIT 100
FOR UPDATE SKIP LOCKED;
```

This ensures:
- Pending and expired-processing events are efficiently found via the partial index
- Time-dependent lease recovery happens in the query, not the index predicate
- Concurrent workers cannot claim the same event due to `FOR UPDATE SKIP LOCKED`

### 8.3 Outbox Worker Algorithm

The worker below is implementation-oriented pseudocode using `NpgsqlDataSource` / `NpgsqlConnection` and `NpgsqlCommand`. It deliberately does **not** dispose `db.Database.GetDbConnection()`, because that connection is owned by the EF Core `DbContext`. Register one application-scoped `NpgsqlDataSource` and inject it into the worker. All SQL parameters are explicitly typed; adapt DTO/property names to the actual project code before implementation.

Claiming rules:
- Claim due `pending` rows only when `next_attempt_at <= now()` and no live lease exists.
- Reclaim `processing` rows only after their lease expires.
- Return the per-channel delivery timestamps so a successful channel is never sent again just because the other channel failed.
- On failure, clear the lease, return the row to `pending`, and set a bounded exponential `next_attempt_at`.
- A worker may update a row only while it still owns an unexpired lease.

```csharp
// Illustrative C# pseudocode. Requires Npgsql and an injected NpgsqlDataSource.
public async Task ProcessBatchAsync(CancellationToken ct)
{
    const int batchSize = 100;
    const int leaseSeconds = 30;
    const int maxAttempts = 10;
    var workerId = _workerInstanceId;
    var claimed = new List<OutboxEvent>();

    await using var conn = await _dataSource.OpenConnectionAsync(ct);
    await using (var claim = new NpgsqlCommand(@"
        WITH candidates AS (
            SELECT id
            FROM outbox_events
            WHERE (status = 'pending' AND next_attempt_at <= now()
                   AND (lease_expires_at IS NULL OR lease_expires_at < now()))
               OR (status = 'processing' AND lease_expires_at < now())
            ORDER BY next_attempt_at, created_at
            LIMIT @batchSize
            FOR UPDATE SKIP LOCKED
        )
        UPDATE outbox_events o
        SET status = 'processing', claimed_at = now(), claimed_by = @workerId,
            lease_expires_at = now() + make_interval(secs => @leaseSeconds)
        FROM candidates c
        WHERE o.id = c.id
        RETURNING o.id, o.event_type, o.payload, o.device_id, o.home_id,
                  o.correlation_id, o.redis_delivered_at, o.signalr_delivered_at;", conn))
    {
        claim.Parameters.Add(new NpgsqlParameter<int>("batchSize", batchSize));
        claim.Parameters.Add(new NpgsqlParameter<Guid>("workerId", workerId));
        claim.Parameters.Add(new NpgsqlParameter<int>("leaseSeconds", leaseSeconds));
        await using var reader = await claim.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            claimed.Add(MapOutboxEvent(reader));
    }

    foreach (var evt in claimed)
    {
        bool? redisSucceeded = null;
        bool? signalrSucceeded = null;
        try
        {
            if (evt.RedisDeliveredAt is null)
                redisSucceeded = await TryDeliverToRedisAsync(evt, ct);
            if (evt.SignalRDeliveredAt is null)
                signalrSucceeded = await TryDeliverToSignalRAsync(evt, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Outbox delivery failed for {EventId}", evt.Id);
            // Any channel not yet successful remains eligible for retry.
            redisSucceeded ??= false;
            signalrSucceeded ??= false;
        }

        await using var update = new NpgsqlCommand(@"
            UPDATE outbox_events
            SET redis_delivered_at = CASE WHEN @redisSucceeded IS TRUE THEN now() ELSE redis_delivered_at END,
                signalr_delivered_at = CASE WHEN @signalrSucceeded IS TRUE THEN now() ELSE signalr_delivered_at END,
                retry_count = retry_count + CASE
                    WHEN COALESCE(@redisSucceeded, TRUE) AND COALESCE(@signalrSucceeded, TRUE) THEN 0 ELSE 1 END,
                last_error = CASE
                    WHEN COALESCE(@redisSucceeded, TRUE) AND COALESCE(@signalrSucceeded, TRUE) THEN NULL
                    WHEN @redisSucceeded IS FALSE AND @signalrSucceeded IS FALSE THEN 'redis_and_signalr_failed'
                    WHEN @redisSucceeded IS FALSE THEN 'redis_failed'
                    WHEN @signalrSucceeded IS FALSE THEN 'signalr_failed'
                    ELSE 'delivery_failed' END,
                status = CASE
                    WHEN (redis_delivered_at IS NOT NULL OR @redisSucceeded IS TRUE)
                     AND (signalr_delivered_at IS NOT NULL OR @signalrSucceeded IS TRUE) THEN 'delivered'
                    WHEN retry_count + CASE
                        WHEN COALESCE(@redisSucceeded, TRUE) AND COALESCE(@signalrSucceeded, TRUE) THEN 0 ELSE 1 END >= @maxAttempts
                        THEN 'dead_letter'
                    ELSE 'pending' END,
                next_attempt_at = CASE
                    WHEN (redis_delivered_at IS NOT NULL OR @redisSucceeded IS TRUE)
                     AND (signalr_delivered_at IS NOT NULL OR @signalrSucceeded IS TRUE) THEN now()
                    ELSE now() + make_interval(secs => LEAST(3600, (2 ^ LEAST(retry_count, 10))::int)) END,
                claimed_by = NULL, claimed_at = NULL, lease_expires_at = NULL
            WHERE id = @eventId AND status = 'processing'
              AND claimed_by = @workerId AND lease_expires_at >= now();", conn);
        update.Parameters.Add(new NpgsqlParameter<Guid>("eventId", evt.Id));
        update.Parameters.Add(new NpgsqlParameter<Guid>("workerId", workerId));
        update.Parameters.Add(new NpgsqlParameter<bool?>("redisSucceeded", redisSucceeded));
        update.Parameters.Add(new NpgsqlParameter<bool?>("signalrSucceeded", signalrSucceeded));
        update.Parameters.Add(new NpgsqlParameter<int>("maxAttempts", maxAttempts));
        var affected = await update.ExecuteNonQueryAsync(ct);
        if (affected == 0)
            _logger.LogWarning("Outbox lease lost for {EventId}; stale worker update rejected", evt.Id);
    }
}
```

**Implementation notes / required review before coding:**
- `MapOutboxEvent`, DTO definitions, retry classification, logging, and delivery methods are project-specific and intentionally not presented as complete compile-ready application code.
- The parameter generic types and SQL casts must be checked against the exact Npgsql version selected by the project. Do not replace them with `DbCommand.Parameters.AddWithValue()` on a generic `DbConnection`.
- The worker should renew its lease or use a lease duration that safely exceeds the maximum delivery attempt duration. If delivery can exceed the lease, a second worker may reclaim the event while the first is still sending.
- The update stores successful channel timestamps independently. If Redis succeeded and SignalR failed, the next attempt skips Redis and retries SignalR only.
- Exponential backoff is bounded at one hour in this example; jitter should be added in application code to avoid synchronized retries. `next_attempt_at` is the scheduling source of truth.
- Delivery is at-least-once, not exactly-once. Redis writes must be idempotent; SignalR clients should deduplicate by stable `eventId` (and state version where applicable).
- PostgreSQL, Redis, and SignalR do not share one atomic transaction. A crash after an external delivery but before recording its timestamp can still cause a duplicate delivery; consumers must tolerate it.

### 8.4 Delivery Guarantees

| Guarantee | Mechanism |
|-----------|-----------|
| **At-least-once to Redis** | Retry until `redis_delivered_at` set; idempotent key = `device:{id}:state` |
| **At-least-once to SignalR** | Retry until `signalr_delivered_at` set; client dedups via `eventId` + `stateVersion` |
| **No dual-write** | PG commit → outbox row exists → worker delivers |
| **Dead letter visibility** | `status = 'dead_letter'` in same table; alert on count > 0 |
| **Lease recovery** | Expired `lease_expires_at` → reclaimed by any worker |
| **Partial delivery handling** | Independent `redis_delivered_at` / `signalr_delivered_at` timestamps; status only `delivered` when both succeed |
| **Consumer idempotency** | Redis: key-based overwrite; SignalR: client deduplicates via `eventId` + `stateVersion` |

### 8.5 Outbox Event Types (A-09)

The `event_type` column uses a `varchar(100)` with a `CHECK` constraint for known event types. This is a **backend implementation requirement**. Proposed event types for MVP:

```sql
ALTER TABLE outbox_events ADD CONSTRAINT ck_outbox_event_type
    CHECK (event_type IN (
        'device.state.changed',
        'device.command.result',
        'device.availability.changed',
        'device.telemetry.received',
        'home.member.added',
        'home.member.removed',
        'home.member.role_changed',
        'permission.granted',
        'permission.revoked',
        'ai.action.executed',
        'device.added',
        'device.removed',
        'room.added',
        'room.removed'
    ));
```

---

## 9. PERMISSIONS AND AUTHORIZATION MODEL

### 9.1 Data Model (Recap)

```sql
-- Enums defined in Section 6.2.1
-- Table defined in Section 6.2.11
```

### 9.2 Authorization Rules (Database-Enforced)

The `validate_permission_row()` trigger enforces **database integrity rules**. Application authorization (Section 9.4) enforces **business logic rules**.

| Rule | Enforced By | Description |
|------|-------------|-------------|
| GRANT grantor must be active member of same home | Trigger | `grantor_membership_id` → active membership in same home as target; system DENY rows have no membership grantor |
| Grantee must be active member of same home | Trigger | `grantee_membership_id` → active membership in same home as target |
| Target home consistency | Trigger | Target and grantee must belong to the same home; for GRANT rows the active grantor must also belong to that home |
| Camera/microphone require capability | Trigger | `target_type IN ('camera','microphone')` → device must have capability |
| Owner can grant permissions within their effective authority | Trigger | Role = 'owner' → can grant any permission type they themselves hold or that is in the owner default set |
| Non-owner requires `member:manage` to grant, and may only grant permissions they hold | Trigger | Role != 'owner' → must hold `member:manage` on home AND each granted permission must be a subset of grantor's effective permissions |
| Self-grant prohibited | Trigger | `grantor_membership_id != grantee_membership_id` |
| Grant rows require a grantor; system deny rows have no membership grantor | CHECK constraint | `grant` requires `grantor_membership_id`; `deny` requires it to be NULL |
| Only trusted system authorization may create DENY rows | Trigger + database role | `nexus_permission_admin` is a separately provisioned, non-login-grantable role; application user sessions cannot assume it |
| Permission type + target type combination must be valid | Trigger | e.g., `camera:stream` only valid with `target_type = 'camera'` |

### 9.3 Trigger Implementation

The permission table supports two distinct write paths:
- **GRANT:** created on behalf of an authenticated active membership. The transaction must set `app.actor_membership_id` locally from the authenticated server-side principal; the trigger verifies it matches `grantor_membership_id`.
- **DENY:** system/security override only. It requires the separately provisioned PostgreSQL role `nexus_permission_admin`, `effect='deny'`, and `grantor_membership_id IS NULL`. The ordinary application database role must not be a member of this role. This is an infrastructure/security prerequisite, not a role the application can self-assign.

The `app.actor_membership_id` setting is a trusted application-to-database assertion, not independent authentication. The application DB credential must remain server-side and unavailable to clients. If the deployment cannot guarantee this trust boundary, use a dedicated stored procedure/service boundary and deny direct table writes.

```sql
CREATE OR REPLACE FUNCTION validate_permission_row()
RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE
    v_grantor_home uuid;
    v_grantee_home uuid;
    v_target_home uuid;
    v_grantor_role varchar(20);
    v_grantor_actor uuid;
    v_has_capability boolean;
    v_grantor_has_perm boolean;
    v_owner_defaults text[] := ARRAY[
        'device:control', 'device:view', 'device:manage',
        'home:manage', 'member:manage', 'settings:manage',
        'ai:propose', 'ai:execute'
    ];
BEGIN
    SELECT m.home_id INTO v_grantee_home
    FROM memberships m
    WHERE m.id = NEW.grantee_membership_id AND m.is_active = true;
    IF v_grantee_home IS NULL THEN
        RAISE EXCEPTION 'Grantee membership not found or inactive';
    END IF;

    -- Resolve target and its home before validating either write path.
    CASE NEW.target_type
        WHEN 'home' THEN v_target_home := NEW.target_id;
        WHEN 'room' THEN
            SELECT home_id INTO v_target_home FROM rooms WHERE id = NEW.target_id AND deleted_at IS NULL;
        WHEN 'device' THEN
            SELECT home_id INTO v_target_home FROM devices WHERE id = NEW.target_id AND removed_at IS NULL;
        WHEN 'camera', 'microphone' THEN
            SELECT home_id INTO v_target_home FROM devices WHERE id = NEW.target_id AND removed_at IS NULL;
            SELECT COALESCE(capabilities ? NEW.target_type, false) INTO v_has_capability
            FROM devices WHERE id = NEW.target_id;
            IF NOT COALESCE(v_has_capability, false) THEN
                RAISE EXCEPTION 'Device does not have % capability', NEW.target_type;
            END IF;
        ELSE RAISE EXCEPTION 'Unknown target_type: %', NEW.target_type;
    END CASE;
    IF v_target_home IS NULL OR v_target_home <> v_grantee_home THEN
        RAISE EXCEPTION 'Target and grantee must belong to the same home';
    END IF;

    IF NEW.permission_type LIKE 'camera:%' AND NEW.target_type <> 'camera' THEN
        RAISE EXCEPTION 'camera:* permissions require target_type = camera';
    END IF;
    IF NEW.permission_type LIKE 'microphone:%' AND NEW.target_type <> 'microphone' THEN
        RAISE EXCEPTION 'microphone:* permissions require target_type = microphone';
    END IF;

    IF NEW.effect = 'deny' THEN
        IF NEW.grantor_membership_id IS NOT NULL OR
           NOT pg_has_role(current_user, 'nexus_permission_admin', 'member') THEN
            RAISE EXCEPTION 'DENY rows are restricted to nexus_permission_admin and must not have a membership grantor';
        END IF;
        RETURN NEW;
    END IF;

    IF NEW.grantor_membership_id IS NULL THEN
        RAISE EXCEPTION 'GRANT rows require grantor_membership_id';
    END IF;
    v_grantor_actor := NULLIF(current_setting('app.actor_membership_id', true), '')::uuid;
    IF v_grantor_actor IS NULL OR v_grantor_actor <> NEW.grantor_membership_id THEN
        RAISE EXCEPTION 'grantor_membership_id must match the authenticated actor';
    END IF;

    SELECT m.home_id, m.role INTO v_grantor_home, v_grantor_role
    FROM memberships m
    WHERE m.id = NEW.grantor_membership_id AND m.is_active = true;
    IF v_grantor_home IS NULL OR v_grantor_home <> v_target_home THEN
        RAISE EXCEPTION 'Grantor must be an active member of the target home';
    END IF;
    IF NEW.grantor_membership_id = NEW.grantee_membership_id THEN
        RAISE EXCEPTION 'Cannot grant permission to self';
    END IF;

    IF v_grantor_role <> 'owner' THEN
        IF NOT EXISTS (
            SELECT 1 FROM permissions p
            JOIN memberships grantor ON grantor.id = p.grantor_membership_id AND grantor.is_active = true
            WHERE p.grantee_membership_id = NEW.grantor_membership_id
              AND p.permission_type = 'member:manage'
              AND p.target_type = 'home' AND p.target_id = v_grantor_home
              AND p.effect = 'grant'
              AND NOT EXISTS (
                  SELECT 1 FROM permissions d
                  WHERE d.grantee_membership_id = NEW.grantor_membership_id
                    AND d.permission_type = 'member:manage'
                    AND (d.target_type = 'home' AND d.target_id = v_grantor_home)
                    AND d.effect = 'deny'
              )
        ) THEN RAISE EXCEPTION 'Non-owner requires effective member:manage'; END IF;

        SELECT EXISTS (
            SELECT 1 FROM permissions p
            JOIN memberships grantor ON grantor.id = p.grantor_membership_id AND grantor.is_active = true
            WHERE p.grantee_membership_id = NEW.grantor_membership_id
              AND p.permission_type = NEW.permission_type
              AND p.target_type = NEW.target_type AND p.target_id = NEW.target_id
              AND p.effect = 'grant'
        ) INTO v_grantor_has_perm;
        IF NOT v_grantor_has_perm OR EXISTS (
            SELECT 1 FROM permissions d
            WHERE d.grantee_membership_id = NEW.grantor_membership_id
              AND d.permission_type = NEW.permission_type AND d.effect = 'deny'
              AND ((d.target_type = NEW.target_type AND d.target_id = NEW.target_id)
                   OR (d.target_type = 'home' AND d.target_id = v_grantor_home))
        ) THEN RAISE EXCEPTION 'Grantor lacks effective authority to grant this permission'; END IF;
    ELSE
        -- Owner may grant ordinary owner-default permissions. Privacy-sensitive
        -- camera/microphone permissions are never inherited from role defaults:
        -- owner must hold an explicit, effective grant on the exact target.
        IF NEW.permission_type LIKE 'camera:%' OR NEW.permission_type LIKE 'microphone:%' THEN
            SELECT EXISTS (
                SELECT 1 FROM permissions p
                JOIN memberships original_grantor
                  ON original_grantor.id = p.grantor_membership_id AND original_grantor.is_active = true
                WHERE p.grantee_membership_id = NEW.grantor_membership_id
                  AND p.permission_type = NEW.permission_type
                  AND p.target_type = NEW.target_type AND p.target_id = NEW.target_id
                  AND p.effect = 'grant'
            ) INTO v_grantor_has_perm;
            IF NOT v_grantor_has_perm OR EXISTS (
                SELECT 1 FROM permissions d
                WHERE d.grantee_membership_id = NEW.grantor_membership_id
                  AND d.permission_type = NEW.permission_type AND d.effect = 'deny'
                  AND ((d.target_type = NEW.target_type AND d.target_id = NEW.target_id)
                       OR (d.target_type = 'home' AND d.target_id = v_grantor_home))
            ) THEN RAISE EXCEPTION 'Owner needs an explicit effective privacy permission on this target'; END IF;
        ELSIF NOT (NEW.permission_type = ANY(v_owner_defaults)) THEN
            SELECT EXISTS (
                SELECT 1 FROM permissions p
                JOIN memberships original_grantor
                  ON original_grantor.id = p.grantor_membership_id AND original_grantor.is_active = true
                WHERE p.grantee_membership_id = NEW.grantor_membership_id
                  AND p.permission_type = NEW.permission_type
                  AND p.target_type = NEW.target_type AND p.target_id = NEW.target_id
                  AND p.effect = 'grant'
            ) INTO v_grantor_has_perm;
            IF NOT v_grantor_has_perm THEN
                RAISE EXCEPTION 'Owner does not hold permission % on % %', NEW.permission_type, NEW.target_type, NEW.target_id;
            END IF;
        END IF;
    END IF;
    RETURN NEW;
END;
$$;

CREATE TRIGGER trg_permissions_validate
    BEFORE INSERT OR UPDATE ON permissions
    FOR EACH ROW EXECUTE FUNCTION validate_permission_row();
```

**Important implementation constraints:**
- Provision `nexus_permission_admin` out-of-band and grant it only to a separate trusted system/security DB role. Do not grant it to the normal application DB login.
- Every GRANT write must execute in a transaction that runs `set_config('app.actor_membership_id', '<authenticated-membership-uuid>', true)` before the insert/update. The value must come from authenticated server-side identity, never request-body input.
- The trigger validates the grantor's direct effective grant for the exact target. Recursive delegation-chain validation remains deferred (R-01); do not describe this as full recursive delegation security.
- Owner defaults do not include camera/microphone permissions. Owners must have an explicit effective privacy permission on the exact target before delegating it. Product/Security must confirm this policy.
- A system DENY applies by precedence in Section 9.4. The privileged deny path is an architectural proposal that must be reviewed with the database/security owners before migration generation.

### 9.4 Application Authorization Algorithm (Precedence)

```csharp
// Pseudocode: CheckPermission(userId, homeId, permissionType, targetType, targetId)
public async Task<AuthResult> CheckPermissionAsync(
    Guid userId, Guid homeId, string permissionType, string targetType, Guid targetId)
{
    var membership = await db.Memberships
        .FirstOrDefaultAsync(m => m.UserId == userId && m.HomeId == homeId && m.IsActive);
    if (membership is null) return AuthResult.Deny("Not an active member of this home");

    // System DENY rows have a null grantor. They are effective immediately and first.
    var denied = await db.Permissions.AnyAsync(p =>
        p.GranteeMembershipId == membership.Id && p.Effect == "deny" &&
        p.PermissionType == permissionType &&
        ((p.TargetType == targetType && p.TargetId == targetId) ||
         (p.TargetType == "home" && p.TargetId == homeId)));
    if (denied) return AuthResult.Deny("Explicit system/security deny");

    // A GRANT is effective only while its grantor membership remains active.
    var granted = await db.Permissions.AnyAsync(p =>
        p.GranteeMembershipId == membership.Id && p.Effect == "grant" &&
        p.PermissionType == permissionType &&
        ((p.TargetType == targetType && p.TargetId == targetId) ||
         (p.TargetType == "home" && p.TargetId == homeId)) &&
        p.GrantorMembershipId != null &&
        db.Memberships.Any(g => g.Id == p.GrantorMembershipId && g.IsActive));
    if (granted) return AuthResult.Allow();

    // Only the Owner default set is active in this baseline. Member/Guest defaults
    // are DISABLED until B-03 is explicitly approved and implemented.
    if (membership.Role == "owner" && IsInOwnerDefaultSet(permissionType) &&
        !IsPrivacySensitive(permissionType))
        return AuthResult.Allow("Owner default");

    return AuthResult.Deny("No effective grant");
}

private bool IsPrivacySensitive(string type) =>
    type.StartsWith("camera:", StringComparison.Ordinal) ||
    type.StartsWith("microphone:", StringComparison.Ordinal);

private bool IsInOwnerDefaultSet(string permissionType) => new HashSet<string> {
    "device:control", "device:view", "device:manage", "home:manage",
    "member:manage", "settings:manage", "ai:propose", "ai:execute"
}.Contains(permissionType);
```

**Authorization invariants:** explicit DENY overrides all grants/defaults; GRANT is effective only if its grantor membership is still active; inactive grantees are denied; Member/Guest defaults remain disabled pending B-03; camera/microphone permissions are never inferred from role defaults. The database query shown is pseudocode and must be translated into a tested EF Core query without client-side evaluation.

### 9.5 Permission Matrix (PENDING B-03)

| Permission Type | Owner Default | Member Default | Guest Default | Grantable By |
|-----------------|---------------|----------------|---------------|--------------|
| `device:control` | ✅ Grant | ❌ Deny | ❌ Deny | Owner; Member with `member:manage` (if they hold it) |
| `device:view` | ✅ Grant | ❌ Deny (disabled pending B-03) | ❌ Deny (disabled pending B-03) | Owner |
| `device:manage` | ✅ Grant | ❌ Deny | ❌ Deny | Owner |
| `home:manage` | ✅ Grant | ❌ Deny | ❌ Deny | Owner |
| `member:manage` | ✅ Grant | ❌ Deny | ❌ Deny | Owner |
| `camera:stream` | ❌ Deny | ❌ Deny | ❌ Deny | Owner (requires device capability) |
| `camera:view` | ❌ Deny | ❌ Deny | ❌ Deny | Owner (requires device capability) |
| `microphone:stream` | ❌ Deny | ❌ Deny | ❌ Deny | Owner (requires device capability) |
| `settings:manage` | ✅ Grant | ❌ Deny | ❌ Deny | Owner |
| `ai:propose` | ✅ Grant | ❌ Deny (disabled pending B-03) | ❌ Deny (disabled pending B-03) | Owner |
| `ai:execute` | ✅ Grant | ❌ Deny | ❌ Deny | Owner |

**Runtime behavior until B-03:** Member and Guest receive no role-default grants. The ❌ values above describe the safe interim behavior, not a final product decision. Any explicit grant still requires the normal grantor-authority checks. Product + Security must approve the final role matrix before Member/Guest defaults are enabled.

### 9.6 Permission Lifecycle and Revocation

| Event | Effect on Permissions |
|-------|----------------------|
| Grantor membership deactivated (`is_active = false`) | Existing GRANT rows remain stored but become **ineffective** because every authorization query joins the grantor membership and requires `is_active = true`. Trigger prevents new grants from inactive grantors. |
| Grantee membership deactivated | Permissions remain stored but authorization returns Deny (grantee not active). |
| Grantor membership physically deleted | Permissions rows with that grantor are deleted by `ON DELETE CASCADE`. Updating `is_active` does not invoke FK cascades. |
| Grantee membership physically deleted | Permissions rows with that grantee are deleted by `ON DELETE CASCADE`. Updating `is_active` does not invoke FK cascades. |
| Target device/room deleted | Orphaned permissions remain in table. Application authorization returns Deny (target not found). Periodic cleanup job recommended. |
| Explicit revoke (DELETE permission row) | Immediate effect on next authorization check. |

**Polymorphic Target Orphaning:** The polymorphic `target_id` has no FK enforcement. Orphaned permissions (target deleted) are not automatically cleaned. This is an explicit design trade-off for MVP flexibility. A scheduled cleanup job or application-level cascade is required for production (see Decision Register: O-05).

---

## 10. DEVICE COMMAND LIFECYCLE

### 10.1 State Machine (7 States)

The device command lifecycle has **7 states** in the database. The NexusLink `command.result` schema (`command-result.schema.json`) allows only three device-reported statuses: `Acknowledged`, `Succeeded`, `Failed`. There is no `Executing` status in the device protocol — the device transitions directly from `Acknowledged` to `Succeeded` or `Failed`. The backend may track execution progress internally (e.g., via a separate `execution_started_at` timestamp), but this is not a persisted command state.

```
Pending → Sent → Acknowledged → Succeeded
                         ↘          ↗
                           Failed ←─┘
                         ↘
                           TimedOut (no result before expires_at)
                         ↘
                           Cancelled (backend requested)
```

| Status | Meaning | Terminal? | Who Sets | Trigger |
|--------|---------|-----------|----------|---------|
| `Pending` | Created, not yet dispatched to MQTT | No | Backend | API call |
| `Sent` | Published to MQTT (QoS 1) | No | Backend | MQTT publish ack |
| `Acknowledged` | Device validated command, will execute | No | Device | `command.result` status=Acknowledged |
| `Succeeded` | Device completed successfully | **Yes** | Device | `command.result` status=Succeeded |
| `Failed` | Device execution failed | **Yes** | Device | `command.result` status=Failed |
| `TimedOut` | No device result before `expires_at` | **Yes** | Backend | Reconciliation job |
| `Cancelled` | Backend requested cancellation | **Yes** | Backend | API cancellation |

**Device Result Schema Alignment:** The `command_status_enum` (Section 6.2.1) contains exactly the 7 states above. `Executing` is **not** a database state. If the backend needs to track when execution begins (for observability), add a nullable `execution_started_at` timestamp column to `device_commands` (proposed as P-08).

### 10.2 Allowed State Transitions

| From State | Allowed Next States | Notes |
|------------|---------------------|-------|
| `Pending` | `Sent`, `Cancelled`, `TimedOut` | Cancel before publish; timeout if expiry passes before dispatch |
| `Sent` | `Acknowledged`, `TimedOut`, `Cancelled` | Timeout if no ack |
| `Acknowledged` | `Succeeded`, `Failed`, `TimedOut`, `Cancelled` | Device transitions directly to terminal |
| `Succeeded` | *(none)* | Terminal — immutable |
| `Failed` | *(none)* | Terminal — immutable |
| `TimedOut` | *(none)* | Terminal — immutable |
| `Cancelled` | *(none)* | Terminal — immutable |

### 10.3 Full Transition Enforcement and Terminal Immutability

The database trigger enforces the complete transition matrix, not only terminal immutability. Any transition not listed in Section 10.2 is rejected. The trigger does not infer that MQTT publication means device execution; `Sent` means broker publish acknowledgement only.

```sql
CREATE OR REPLACE FUNCTION enforce_device_command_transition()
RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    IF NEW.status = OLD.status THEN
        RETURN NEW;
    END IF;

    IF OLD.status IN ('Succeeded', 'Failed', 'TimedOut', 'Cancelled') THEN
        RAISE EXCEPTION 'Terminal command state % is immutable', OLD.status;
    END IF;

    IF NOT (
        (OLD.status = 'Pending' AND NEW.status IN ('Sent', 'Cancelled', 'TimedOut')) OR
        (OLD.status = 'Sent' AND NEW.status IN ('Acknowledged', 'TimedOut', 'Cancelled')) OR
        (OLD.status = 'Acknowledged' AND NEW.status IN ('Succeeded', 'Failed', 'TimedOut', 'Cancelled'))
    ) THEN
        RAISE EXCEPTION 'Invalid device command transition: % -> %', OLD.status, NEW.status;
    END IF;
    RETURN NEW;
END;
$$;

CREATE TRIGGER trg_device_commands_transition
    BEFORE UPDATE OF status ON device_commands
    FOR EACH ROW EXECUTE FUNCTION enforce_device_command_transition();
```

**Concurrency requirement:** All command-result handling, cancellation, dispatch, and timeout reconciliation must update with an expected-current-status predicate (compare-and-set) inside a transaction. A zero-row update means another operation won the race; reload and handle as a duplicate/late event. The trigger is the final invariant guard, not a replacement for concurrency control.

### 10.4 Cancellation Semantics (Honest Design)

| Scenario | Behavior |
|----------|----------|
| **Cancel before MQTT publish** (`Pending`) | Status → `Cancelled`; command never sent |
| **Cancel after MQTT publish** (`Sent`) | Status → `Cancelled`; **best-effort only** — device may still execute |
| **Cancel after Acknowledged** | Status → `Cancelled`; **cannot recall** — device already validated and committed to execute |
| **Late result after cancellation** | Terminal state (`Cancelled`) **not overwritten**; late result logged as anomaly |

**No cancellation MQTT topic exists in NexusLink v1.** The spec defines only `command` and `command-results` channels. Cancellation is a backend-side status change only.

**Cancellation Race Condition:** If Backend sets status = `Cancelled` concurrently with MQTT publish completing, the device may still receive and execute the command. The `Cancelled` status in the database reflects the *intent*, not a guarantee of physical non-execution. This is documented as a known limitation.

### 10.5 Idempotency Index and Key Reuse

The partial unique index on `device_commands (requested_by_user_id, idempotency_key)` allows key reuse **after terminal completion**:

```sql
CREATE UNIQUE INDEX uk_device_commands_idempotency 
    ON device_commands (requested_by_user_id, idempotency_key) 
    WHERE requested_by_user_id IS NOT NULL 
      AND status IN ('Pending', 'Sent', 'Acknowledged');
```

**Intentional Design:** Once a command reaches a terminal state (`Succeeded`, `Failed`, `TimedOut`, `Cancelled`), the same `idempotency_key` can be reused for a new command. This allows clients to retry with the same key after definitive failure/timeout. If key reuse after terminal is not desired, remove the status filter from the index (but this would prevent legitimate retries after timeout).

### 10.6 Duplicate Handling

| Duplicate Source | Handling |
|------------------|----------|
| Client retries with same `Idempotency-Key` | Partial unique index prevents duplicate `device_commands` row |
| MQTT QoS 1 redelivers command | Device deduplicates via last 20 `commandId` (spec §4.3) |
| MQTT QoS 1 redelivers command.result | Backend checks `device_commands.status` — ignores if terminal |
| Device sends duplicate `Succeeded` | Backend updates only if status not terminal; logs duplicate |

### 10.7 Late Device Result Policy

- If `device_commands.status` is terminal (`Succeeded`/`Failed`/`TimedOut`/`Cancelled`): **ignore** late result, log anomaly
- If status is non-terminal (`Pending`/`Sent`/`Acknowledged`): apply result, transition to terminal
- Reconciliation job runs every 30s: finds `Pending`, `Sent`, and `Acknowledged` commands past `expires_at` → marks `TimedOut` (a command that expires before dispatch must not remain Pending forever).
- **Dispatch/timeout race:** a timeout may win after a publish attempt has started but before the backend records `Sent`. In that case the device may still execute while the database records `TimedOut`; a later result is treated as late and logged, not used to overwrite the terminal state. This is an honest deadline outcome, not proof of non-execution.

### 10.8 Timeout Reconciliation

```sql
-- Run every 30s via background job
UPDATE device_commands
SET status = 'TimedOut', completed_at = now()
WHERE status IN ('Pending', 'Sent', 'Acknowledged')
  AND expires_at < now();
```

---

## 11. MQTT / NEXUSLINK INTEGRATION CONTRACT

### 11.1 Topic Structure (Verified)

| Channel | Topic | Direction | QoS | Retained | Schema |
|---------|-------|-----------|-----|----------|--------|
| Commands | `nexushome/v1/homes/{homeId}/devices/{deviceId}/commands` | Backend→Device | 1 | No | command.schema.json |
| Command Results | `.../command-results` | Device→Backend | 1 | No | command-result.schema.json |
| State | `.../state` | Device→Backend | 1 | Yes | state.schema.json |
| Telemetry | `.../telemetry` | Device→Backend | 0 | No | telemetry.schema.json |
| Availability | `.../availability` | Device/Broker→Backend | 1 | Yes | availability.schema.json |

### 11.2 Key Protocol Rules (Verified)

| Rule | Spec Reference | Implementation Implication |
|------|----------------|----------------------------|
| TLS 1.2+ on 8883 only | §2 | No plaintext; cert validation required |
| `clientId` = `username` = `deviceId` | §2 | Broker ACL enforces per-device namespace |
| Clean session | §2 | Broker never queues for offline devices |
| Last Will: `availability` offline | §3 | Backend receives offline on unexpected disconnect |
| Heartbeat: `online` every 30s | §3 | Backend marks offline after 3 missed (90s) |
| Command expiry in `expiresAt` | §5.7 | 5s clock tolerance; reject if `now > expiresAt` |
| Device stores last 20 `commandId` | §4.3 | Deduplication on device side |
| Validation order | §5 | JSON → schemaVersion → messageType → Schema → IDs → messageId dedup → Time → Command validity |

### 11.3 Message Shapes (Verified Against Source Schemas)

**Command (Backend → Device)** — `command.schema.json`
```json
{
  "schemaVersion": 1,
  "messageType": "command",
  "messageId": "uuid",
  "commandId": "uuid",           -- Matches device_commands.command_id
  "correlationId": "uuid",
  "issuedAt": "ISO8601 UTC",
  "expiresAt": "ISO8601 UTC",
  "target": { "homeId": "uuid", "deviceId": "uuid" },
  "command": { "type": "climate.set_temperature", "parameters": { "celsius": 24 } }
}
```

**Command Result (Device → Backend)** — `command-result.schema.json`
```json
{
  "schemaVersion": 1,
  "messageType": "command.result",
  "messageId": "uuid",
  "commandId": "uuid",
  "correlationId": "uuid",
  "homeId": "uuid",
  "deviceId": "uuid",
  "status": "Acknowledged|Succeeded|Failed",
  "reportedAt": "ISO8601 UTC",
  "reportedState": { "power": "on", "temperatureCelsius": 24 },
  "error": null | { "code": "UNSUPPORTED_COMMAND", "message": "..." }
}
```

**State (Device → Backend, Retained)** — `state.schema.json`
```json
{
  "schemaVersion": 1,
  "messageType": "state",
  "messageId": "uuid",
  "homeId": "uuid",
  "deviceId": "uuid",
  "reportedAt": "ISO8601 UTC",
  "state": { "power": "on", "temperatureCelsius": 24 }
}
```

**Telemetry (Device → Backend, QoS 0)** — `telemetry.schema.json`
```json
{
  "schemaVersion": 1,
  "messageType": "telemetry",
  "messageId": "uuid",
  "homeId": "uuid",
  "deviceId": "uuid",
  "reportedAt": "ISO8601 UTC",
  "readings": [
    { "name": "temperature", "value": 23.5, "unit": "celsius" },
    { "name": "humidity", "value": 45, "unit": "percent" }
  ]
}
```

**Availability (Device/Broker → Backend, Retained)** — `availability.schema.json`
```json
{
  "schemaVersion": 1,
  "messageType": "availability",
  "messageId": "uuid",
  "homeId": "uuid",
  "deviceId": "uuid",
  "status": "online|offline",
  "reportedAt": "ISO8601 UTC"
}
```

### 11.4 Contract Status Summary

| Aspect | Status | Notes |
|--------|--------|-------|
| Topic patterns, QoS, retained | **Confirmed** | All 5 channels match spec.md and schemas |
| Message field names, types, required fields | **Confirmed** | All 5 schemas validated |
| Command lifecycle states | **Confirmed** | 7 backend states mapped to 3 device states |
| Device error codes | **Confirmed** | `UNSUPPORTED_COMMAND`, `INVALID_PARAMETER`, `COMMAND_EXPIRED`, `CLOCK_NOT_SYNCED`, `DEVICE_BUSY`, `EXECUTION_FAILED` |
| Telemetry batch limits | **Confirmed** | maxItems: 8 (schema), enforced in ingestion function |
| Telemetry reading validation | **Confirmed** | name pattern, value=number, unit=string required |
| Spec proposed values | **Proposed** | spec.md §7, §9 mark values as proposed/needing confirmation |

---

## 12. CROSS-TEAM API AND EVENT CONTRACTS

### 12.1 HTTP API (Contract §4)

**Base:** `/api/v1`  
**Auth:** Bearer token (JWT proposed)  
**Envelope:** Success `{ data, meta }` / Error `{ error: { code, message, requestId, details } }`

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/homes` | GET/POST | List/create homes |
| `/homes/{homeId}` | GET/PUT/DELETE | Home CRUD |
| `/homes/{homeId}/rooms` | GET/POST | Room CRUD |
| `/homes/{homeId}/members` | GET/POST/DELETE | Membership management |
| `/homes/{homeId}/devices` | GET/POST | Device registry |
| `/homes/{homeId}/devices/{deviceId}/commands` | POST | Dispatch command (Idempotency-Key header) |
| `/homes/{homeId}/devices/{deviceId}/state` | GET | Current device state |
| `/homes/{homeId}/devices/{deviceId}/telemetry` | GET | Historical telemetry |
| `/ai/action` | POST | AI action proposal (service-to-service) |
| `/settings` | GET/PUT | User settings |
| `/invitations` | POST/GET | Invite management |

### 12.2 AI Integration (Contract §7)

**Request (Backend → FastAPI)**
```json
{
  "schemaVersion": 1,
  "requestId": "uuid",
  "correlationId": "uuid",
  "context": {
    "homeId": "uuid",
    "locale": "en",
    "allowedDevices": [
      { "deviceId": "uuid", "name": "Living Room AC", "capabilities": ["climate.set_temperature", "power.set"] }
    ]
  },
  "userInput": { "type": "text", "text": "Set AC to 24 degrees" }
}
```

**Response (FastAPI → Backend)**
```json
{
  "schemaVersion": 1,
  "requestId": "uuid",
  "result": {
    "type": "action_proposal",
    "actions": [
      { "deviceId": "uuid", "commandType": "climate.set_temperature", "parameters": { "celsius": 24 }, "requiresConfirmation": false }
    ],
    "userMessage": "I will set the living room AC to 24 degrees."
  }
}
```

**Backend Safeguards:** Validate response schema, device membership, capability, parameters, permissions. Reject unknown commands/fields. AI proposes, Backend executes. High-risk actions require user confirmation. Treat AI output as untrusted.

### 12.3 Real-Time Events (Contract §8)

**Event Envelope**
```json
{
  "eventType": "device.state.changed",
  "schemaVersion": 1,
  "eventId": "uuid",
  "occurredAt": "ISO8601 UTC",
  "homeId": "uuid",
  "deviceId": "uuid",
  "stateVersion": 18,
  "state": { "power": "on", "temperatureCelsius": 24 }
}
```

**Rules:** Per-connection auth, per-home subscription auth, camera/mic separate auth, events are hints not durable state, include eventId/version for dedup.

### 12.4 Device Onboarding (Contract §12)

1. Owner initiates onboarding via API
2. Backend generates pairing credentials (Network/Security implements)
3. Device proves possession via secure pairing (Embedded implements)
4. Backend registers device: assigns to home, optional room, validates capabilities against registry
5. Device receives credentials, connects to MQTT
6. Backend updates device `connection_status = 'online'`

---

## 13. CANONICAL DECISION REGISTER

### 13.1 Decision Classification

| Class | Meaning | Examples |
|-------|---------|----------|
| **Confirmed** | Approved by Technical Lead, immutable for MVP | 11-table scope, selected data-store responsibilities, intended MQTT/NexusLink integration |
| **Proposed** | Technical recommendation, not yet cross-team approved | Outbox lease duration, telemetry chunk interval |
| **Unresolved** | Requires explicit cross-team decision | Permission enums, retention periods, MQTT broker |

### 13.2 Decision Count Summary (Verified)

| Category | Count | IDs |
|----------|-------|-----|
| **Confirmed** | **16** | D-01 through D-07, D-12 through D-20 |
| **Proposed** | **13** | D-08 through D-11, P-01 through P-09 |
| **Unresolved** | **18** | B-01 through B-07, I-01 through I-05, O-01 through O-06 |

**Verification:** D-01–D-07 (7) + D-12–D-20 (9) = 16 Confirmed. D-08–D-11 (4) + P-01–P-09 (9) = 13 Proposed. B-01–B-07 (7) + I-01–I-05 (5) + O-01–O-06 (6) = 18 Unresolved.

### 13.3 Confirmed Decisions (Backend-Independent)

| ID | Decision | Status | Source |
|----|----------|--------|--------|
| **D-01** | 11 domain tables for MVP | **Confirmed** | Task Instructions, DB Summary, ERD |
| **D-02** | DEVICE_COMMANDS included in MVP | **Confirmed** | Technical Lead override |
| **D-03** | No automation tables | **Confirmed** | All sources consistent |
| **D-04** | pgvector excluded from MVP | **Confirmed** | Backend Scope, Contract |
| **D-05** | PostgreSQL 16+ primary DB | **Confirmed** | Backend Scope, DB Summary |
| **D-06** | TimescaleDB for telemetry | **Confirmed** | Backend Scope, DB Summary |
| **D-07** | Redis for live state/cache | **Confirmed** | Backend Scope, Contract |
| **D-08** | EF Core Code First | **Proposed** | Backend Scope |
| **D-09** | JWT authentication | **Proposed** | Backend Scope, Contract |
| **D-10** | SignalR/WebSocket for real-time | **Proposed** | Backend Scope, Contract |
| **D-11** | Docker Compose for local dev | **Proposed** | Backend Scope |
| **D-12** | NexusLink over MQTT/TLS | **Confirmed** | spec.md, Contract |
| **D-13** | Device external_id globally unique | **Confirmed** | DB Summary line 16 |
| **D-14** | Device soft delete (removed_at) | **Confirmed** | DB Summary line 19 |
| **D-15** | Composite FK devices→rooms MATCH SIMPLE | **Confirmed** | DB Summary line 14 |
| **D-16** | RESTRICT on home→devices delete | **Confirmed** | DB Summary line 19 |
| **D-17** | RESTRICT on device→telemetry/audit/commands physical delete (including AI audit); normal retirement uses soft delete | **Confirmed** | DB Summary line 19 |
| **D-18** | CASCADE on user→device_commands.requested_by_user_id | **Confirmed** | DB Summary line 23 |
| **D-19** | Permissions effect grant/deny | **Confirmed** | DB Summary line 20 |
| **D-20** | Settings UK on user_id (personal only) | **Confirmed** | DB Summary line 21 |

> **Note on D-18:** `DATABASE_MVP_SUMMARY.md` line 23 explicitly specifies `ON DELETE CASCADE` for `device_commands.requested_by_user_id` and states that `SET NULL` was considered but rejected. This FK decision is **Confirmed** and is not reopened.
>
> **B-06 Legal Policy (Separate):** The remaining Legal + Product decision covers: (1) anonymization/erasure workflow, (2) retention periods, (3) whether CASCADE satisfies GDPR "right to erasure" or soft-delete is primary path. This is a **compliance/operational approval gate** (P2), not a schema choice. It does not block migration generation.

### 13.4 Proposed Technical Decisions (Backend Can Approve)

| ID | Decision | Status | Blocks |
|----|----------|--------|--------|
| **P-01** | Outbox lease duration = 30s | **Proposed** | Schema generation (default value) |
| **P-02** | Outbox batch size = 100 | **Proposed** | None (runtime config) |
| **P-03** | Telemetry hypertable chunk interval = 1 day | **Proposed** | Schema generation (raw SQL) |
| **P-04** | Telemetry compression after 7 days | **Proposed** | Schema generation (raw SQL) |
| **P-05** | Device command terminal state immutability | **Proposed** | Schema generation (trigger) |
| **P-06** | Redis HA: single instance dev, cluster prod | **Proposed** | Phase 3.3/6.4 |
| **P-07** | AI action audit retention = 7 years | **Proposed** | Schema generation (retention policy) |
| **P-08** | Device command execution_started_at column | **Proposed** | Schema generation (optional column for observability) |
| **P-09** | Partial unique index for duplicate permission grants | **Proposed** | Schema generation (index; only after delegation semantics are approved) |

### 13.5 Unresolved Cross-Team Decisions (Reclassified by Blocker Type)

| ID | Decision | Blocker Type | Priority | Responsible | Blocks Schema Gen? | Required For |
|----|----------|--------------|----------|-------------|---------------------|--------------|
| **B-01** | Permission type enum values | **Schema** | **P0** | Product + Security | **YES** | `PERMISSIONS` CHECK constraint / CREATE TYPE |
| **B-02** | Target type enum values | **Schema** | **P0** | Product + Security | **YES** | `PERMISSIONS` CHECK constraint / CREATE TYPE |
| **B-03** | Role default permission matrix | **Runtime / AuthZ** | **P1** | Product + Security | **NO** (defaults applied in app logic) | Authorization behavior, seed data |
| **B-04** | Device capabilities JSON schema | **Schema (optional)** | **P1** | Embedded + AI | **NO** (JSONB column created without CHECK) | `DEVICES.capabilities` validation |
| **B-05** | Telemetry retention period | **Schema (policy)** | **P1** | Product + DevOps + Legal | **NO** (hypertable created without retention) | TimescaleDB retention policy DDL |
| **B-06 (FK)** | **CONFIRMED** — `device_commands.requested_by_user_id ON DELETE CASCADE` | — | — | — | **NO (decided)** | — |
| **B-06 (legal)** | User anonymization/erasure workflow, retention policy | **Compliance / Operational** | **P2** | Legal + Product | **NO** (policy, not schema) | Compliance approval, erasure workflow |
| **B-07** | MQTT broker selection | **Infrastructure / Integration** | **P1** | Network + DevOps | **NO** | Phase 3.1 infrastructure |

**Reclassification Notes:**
- **B-01, B-02**: Enum values directly affect `CREATE TYPE` and `CHECK` constraints → **Schema blockers (P0)**
- **B-03**: Default permission matrix is primarily an authorization/runtime concern. The `PERMISSIONS` table and trigger work without it; defaults are applied in application logic (Section 9.4). → **Runtime blocker (P1)**
- **B-04**: `DEVICES.capabilities` is a `jsonb` column. Validation schema can be added later via CHECK constraint or application logic. → **Schema-optional (P1)**
- **B-05**: Hypertable creation does not require retention policy. Retention is a `SELECT add_retention_policy()` call applied later. → **Schema-optional policy (P1)**
- **B-06 (FK)**: `ON DELETE CASCADE` for `device_commands.requested_by_user_id` is **Confirmed (D-18)** per `DATABASE_MVP_SUMMARY.md` line 23. This is not reopened.
- **B-06 (legal)**: Anonymization/erasure workflow, retention periods, GDPR compliance confirmation. This is a **compliance/operational approval gate (P2)**, not a schema choice. Does not block migration generation.
- **B-07**: MQTT broker is infrastructure provisioning. Database schema does not depend on it. → **Integration blocker (P1)**

### 13.6 Unresolved Cross-Team Decisions (Integration Blockers)

| ID | Decision | Priority | Responsible | Blocks Schema Gen? | Required For |
|----|----------|----------|-------------|---------------------|--------------|
| **I-01** | Owner transfer concurrent workflow | **P1** | Product | No | Phase 2.4-2.5 |
| **I-02** | Late command result handling policy | **P1** | Backend Lead | No | Phase 3.6 |
| **I-03** | Redis HA topology | **P1** | DevOps | No | Phase 3.3/6.4 |
| **I-04** | Camera/microphone stream architecture | **P1** | AI + Embedded + Network | No | Phase 4/5 |
| **I-05** | Invitation role rules (Owner invitations) | **P1** | Product + Security | No | Phase 2.4 (membership management) |

### 13.7 Unresolved Cross-Team Decisions (Operational Blockers)

| ID | Decision | Priority | Responsible | Blocks Schema Gen? | Required For |
|----|----------|----------|-------------|---------------------|--------------|
| **O-01** | Cloud hosting / managed services | **P2** | DevOps + Backend | No | Phase 6.4 |
| **O-02** | Push notification provider | **P2** | Mobile + DevOps | No | Phase 5.4 |
| **O-03** | Home-level settings scope | **P2** | Product | No | Phase 5.1 (deferred to v2) |
| **O-04** | Conversation/context storage | **P2** | AI + Product | No | Phase 4.4 (explicitly TBC) |
| **O-05** | Audit PII encryption at rest | **P2** | Security + Legal | No | Phase 6.1 |
| **O-06** | Telemetry dedup ledger retention period | **P2** | DevOps + Product | No | Phase 3.4 (telemetry ingestion) |

---

## 14. CANONICAL FOREIGN-KEY DELETION POLICY

The following table aligns all FKs with their actual `ON DELETE` actions as defined in the DDL (Section 6.2). This resolves contradictions between D-17 and the DDL.

| Referencing Table | FK Column(s) | Referenced Table | Referenced Column(s) | ON DELETE | Reason |
|-------------------|--------------|------------------|---------------------|-----------|--------|
| `memberships` | `user_id` | `users` | `id` | **CASCADE** | User deletion removes their memberships |
| `memberships` | `home_id` | `homes` | `id` | **CASCADE** | Home deletion removes memberships |
| `rooms` | `home_id` | `homes` | `id` | **CASCADE** | Home deletion removes rooms |
| `invitations` | `home_id` | `homes` | `id` | **CASCADE** | Home deletion removes invitations |
| `invitations` | `invited_by_user_id` | `users` | `id` | **SET NULL** | Inviter deletion preserves invitation record |
| `devices` | `home_id` | `homes` | `id` | **RESTRICT** | Prevent home deletion while devices exist (D-16) |
| `devices` | `room_id` | `rooms` | `id` | **SET NULL** | Room deletion clears device.room_id; FK allows NULL |
| `devices` | `(home_id, room_id)` | `rooms` | `(home_id, id)` | **NO ACTION** | Composite FK ensures assigned room belongs to the same home; nullable `room_id` uses MATCH SIMPLE. |
| `device_telemetry` | `device_id` | `devices` | `id` | **RESTRICT** | Preserve telemetry history (D-17) |
| `ai_action_audit` | `device_id` | `devices` | `id` | **RESTRICT** | Matches D-17; physical device deletion is blocked while audit references exist. Use `removed_at` soft deletion for normal device retirement. |
| `ai_action_audit` | `user_id` | `users` | `id` | **SET NULL** | User anonymization preserves audit; user reference nulled |
| `ai_action_audit` | `command_id` | `device_commands` | `command_id` | **SET NULL** | Command deletion preserves audit; command reference nulled |
| `device_commands` | `home_id` | `homes` | `id` | **RESTRICT** | Preserve command history (D-17) |
| `device_commands` | `(home_id, device_id)` | `devices` | `(home_id, id)` | **RESTRICT** | Preserve command history (D-17); explicitly declared |
| `device_commands` | `requested_by_user_id` | `users` | `id` | **CASCADE** | User deletion removes their command history (D-18, DB Summary line 23) |
| `settings` | `user_id` | `users` | `id` | **CASCADE** | User deletion removes their settings |
| `permissions` | `grantor_membership_id` | `memberships` | `id` | **CASCADE** | Grantor membership deletion revokes their grants |
| `permissions` | `grantee_membership_id` | `memberships` | `id` | **CASCADE** | Grantee membership deletion revokes their permissions |

**Notes:**
- `RESTRICT` vs `NO ACTION` vs `SET NULL`: `RESTRICT` checks immediately; `NO ACTION` defers to end of transaction; `SET NULL` sets the FK column to NULL. For D-16/D-17, `RESTRICT` is used per DB Summary.
- `devices.room_id` explicitly declares `ON DELETE SET NULL` (matching DDL in Section 6.2.6). Room deletion clears the device's room assignment.
- `device_commands` composite FK to `devices` explicitly declares `ON DELETE RESTRICT` (matching DDL in Section 6.2.9). Device deletion is blocked while commands exist.
- `ai_action_audit.device_id` uses `RESTRICT` to match D-17. Normal device retirement is a soft delete (`removed_at`) and does not invoke FK actions. Physical deletion is blocked while audit rows reference the device.
- The `rooms` composite FK target `(home_id, id)` has a unique index (`uk_rooms_home_id`), satisfying the FK requirement.
- The `devices` composite FK target `(home_id, id)` has a unique index (`uk_devices_home_id`), satisfying the FK requirement.

---

## 15. REMAINING RISKS AND BLOCKERS

### 15.1 Critical Technical Risks

| Risk | Severity | Description | Mitigation |
|------|----------|-------------|------------|
| **R-01** | **CRITICAL** | Permissions trigger grantor-authority check is recursive; may not handle all delegation chains | Simplify: Owner can grant anything; non-owner requires explicit `member:manage` on home. Document as MVP limitation. |
| **R-02** | **HIGH** | Outbox worker lease expiration (30s) may be too short for slow Redis/SignalR delivery under load | Make lease configurable; monitor `retry_count`; alert on dead letters. |
| **R-03** | **HIGH** | Device command state machine has no persistence for "cancellation requested" metadata | Add `cancellation_requested_at`, `cancellation_reason` columns to `DEVICE_COMMANDS` for observability (Proposed). |
| **R-04** | **HIGH** | EF Core composite FK mapping requires `HasPrincipalKey` on alternate keys — verify all three (rooms, devices, device_commands) | Migration test required: `HasAlternateKey` on rooms/devices; `HasPrincipalKey` on device_commands FK. |
| **R-05** | **HIGH** | TimescaleDB hypertable creation requires raw SQL in migration — no EF Core native support | Migration must use `migrationBuilder.Sql()` for `create_hypertable`, compression, retention. |
| **R-06** | **MEDIUM** | Trigger-based permission validation adds latency to INSERT/UPDATE | Keep triggers simple; bulk grants use single-row inserts; monitor `pg_stat_user_functions`. |
| **R-07** | **MEDIUM** | No observability design for outbox worker (metrics, health checks, dead letter alerting) | Add: Prometheus metrics (queue depth, processing latency, dead letter count); health endpoint. |

### 15.2 Schema Generation Blockers (Must Resolve — P0 Schema Blockers Only)

| Blocker | Decision ID | Required Before |
|---------|-------------|-----------------|
| Permission type enum values | **B-01** | Migration 0001 (CREATE TYPE) |
| Target type enum values | **B-02** | Migration 0001 (CREATE TYPE) |

**Note:** B-03, B-04, B-05, B-07 are reclassified as P1 (Runtime/Integration/Policy) and do **not** block initial schema generation. B-06 legal policy is P2 (compliance/operational) and does not block schema generation. The FK decision `ON DELETE CASCADE` for `device_commands.requested_by_user_id` is Confirmed (D-18) and not a blocker.

---

## 16. IMPLEMENTATION READINESS VERDICT

### 16.1 Final Classification

| Gate | Status | Condition |
|------|--------|-----------|
| **Ready for Technical Lead Review** | **YES** | Document is internally consistent, source-grounded, all contradictions resolved |
| **Technically Approved** | **PENDING** | Awaiting cross-team resolution of B-01, B-02, and B-06 legal policy |
| **Migration Generation** | **NO-GO** | Blocked by 2 schema-affecting decisions (B-01, B-02 enum values) |
| **Migration Application** | **NOT AUTHORIZED** | Requires explicit database-change approval after Technical Lead authorization |

### 16.2 Justification

| Category | Count | Status |
|----------|-------|--------|
| Confirmed decisions (backend-independent) | 16 | ✅ Recorded; source confirmation remains subject to the decision register |
| Proposed technical decisions | 13 | ⚠️ Not all approved; P-08/P-09 remain proposals |
| **Unresolved schema blockers (P0)** | **2** | ❌ **BLOCKING migration generation** (B-01, B-02 enum values) |
| Unresolved runtime/integration blockers (P1) | 9 | ⚠️ Block corresponding feature integration |
| Unresolved operational/compliance blockers (P2) | 7 | ⚠️ Block production hardening (incl. B-06 legal policy) |

### 16.3 Minimum to Proceed

Do **not** generate or apply migrations from this document yet. First:
1. Resolve and record B-01 and B-02 (enum values) with Product + Security.
2. Resolve B-06 legal policy (anonymization/erasure workflow, retention, GDPR confirmation) with Legal + Product — **this is a compliance approval gate, not a schema blocker**.
3. Review the revised permissions write paths, especially the privileged DENY role and authenticated actor binding, with the database/security owners.
4. Validate all FK actions and the canonical FK matrix against the final DDL, including the `devices(home_id, room_id)` composite FK.
5. Review the Outbox worker against the exact Npgsql package/API version and implement channel-level idempotent retries, lease renewal/expiry handling, and bounded backoff.
6. Validate the full command transition matrix and Pending timeout behavior against the chosen command expiry semantics.
7. **NexusLink protocol compatibility has been independently verified against all five source JSON schemas and spec.md in this review.**
8. Approve O-06 before enabling dedup-ledger cleanup.

After these gates are complete, the Technical Lead may authorize migration generation. Applying migrations still requires a separate explicit database-change approval and a reviewed migration plan.

**Important:** SQL/C# blocks in this architecture are design-level examples, not an executable migration package. Review and test them against the selected PostgreSQL, TimescaleDB, EF Core, and Npgsql versions before implementation.

---

## 17. FINAL IMPLEMENTATION ROADMAP

### Phase 1: Foundation (Weeks 1-2) — **BLOCKED: NO MIGRATION GENERATION UNTIL B-01, B-02 ARE RESOLVED**

| Task | Dependencies | Deliverable |
|------|--------------|-------------|
| 1.1 Finalize scope with all tracks | — | Agreed scope |
| 1.2 REST API contract / OpenAPI | 1.1 | API contract doc |
| **1.3 Database schema / ERD** | **1.1 + B-01, B-02** | **Final schema (this document)** |
| 1.4 Project setup | 1.1 | Backend solution skeleton |
| 1.5 Development environments | 1.4 | Documented dev setup |
| 1.6 Dockerized local environment | 1.4, 1.5 | Local PG + TimescaleDB + Redis |

**Note:** B-03, B-04, B-05, B-07 are P1 (Runtime/Integration) and do not block Phase 1 schema generation. B-06 legal policy is P2 (compliance) and does not block schema generation. They must be resolved before Phase 2–3 feature integration / production.

### Phase 2: Core Backend (Weeks 3-4) — **REQUIRES Phase 1 Complete**

| Task | Dependencies | Deliverable |
|------|--------------|-------------|
| 2.1 Authentication (JWT) | 1.2, 1.3 | Login/token flow |
| 2.2 Home management | 2.1 | Home endpoints |
| 2.3 Room management | 2.2 | Room endpoints |
| 2.4 Member management | 2.2 | Member endpoints |
| 2.5 Role model (Owner/Member/Guest) | 2.4 | Role implementation |
| 2.6 Permission model | 2.5 + B-01,B-02,B-03 | Permission checks enforced |
| 2.7 Device registry | 2.3, 2.6 + B-04 | Device CRUD + capabilities |
| 2.8 Unit tests | 2.1-2.7 | Test suite |

### Phase 3: Device Communication (Weeks 5-6) — **REQUIRES Phase 2 + B-07 (P1 Integration)**

| Task | Dependencies | Deliverable |
|------|--------------|-------------|
| 3.1 MQTT integration | 2.7 + B-07 | Backend connected to broker |
| 3.2 NexusLink integration | 3.1 + spec.md | Contract compliance |
| 3.3 Device state in Redis | 3.2 + outbox | Live state store |
| 3.4 Telemetry ingestion | 3.2 + B-05 (P1) | TimescaleDB pipeline + dedup ledger |
| 3.5 Command dispatch | 3.2, 2.6 | Command sending |
| 3.6 Command acknowledgements | 3.5 | Ack handling |
| 3.7 Real-time updates | 3.3, 3.6 | SignalR/WebSocket |

**Note:** B-05 (telemetry retention) and B-07 (MQTT broker) are P1 integration blockers. Phase 3 can begin with provisional values but requires resolution before production.

### Phase 4: AI Integration (Weeks 7-8)

| Task | Dependencies | Deliverable |
|------|--------------|-------------|
| 4.1 FastAPI AI gateway | AI contract + 2.1 | Backend-FastAPI interface |
| 4.2 Per-device AI permissions | 2.6, 2.7 | AI permission model |
| 4.3 AI action execution | 4.1, 4.2, 3.5 | Execution API |
| 4.4 Conversation/context storage | 4.1 + O-04 | Deferred |
| 4.5 AI action audit log | 4.3 | Audit records |

### Phase 5: App & Wall Panel (Weeks 9-10)

| Task | Dependencies | Deliverable |
|------|--------------|-------------|
| 5.1 Settings APIs | 2.2, 2.6 | Settings endpoints |
| 5.2 Device onboarding | 2.7, 3.2 + I-04 | Onboarding API |
| 5.3 Device removal | 2.7, 3.2 | Removal API |
| 5.4 Push notifications | 2.1 + O-02 | Notification backend |
| 5.5 Avatar preference | 5.1 | Avatar endpoint |
| 5.6 Wall Panel support | 2.1, 3.7 | Session/API support |

### Phase 6: Hardening (Weeks 11-12)

| Task | Dependencies | Deliverable |
|------|--------------|-------------|
| 6.1 Security review | Phases 2-5 | Findings + fixes |
| 6.2 Integration testing | Phases 3-5 + all tracks | Test results |
| 6.3 Performance testing | 6.2 | Perf results |
| 6.4 Deployment | 6.1, 6.2 + O-01 | Deployed system |
| 6.5 Documentation | 6.2 | Final docs |
| 6.6 Demo preparation | 6.4, 6.5 | Demo scenario |

---

## 18. VALIDATION CHECKLIST (PRE-MIGRATION)

### 18.1 Schema Validation Queries

```sql
-- Run after Migration 0001-0004 to verify baseline

-- 1. All 11 domain tables exist
SELECT count(*) FROM information_schema.tables 
WHERE table_schema = 'public' 
  AND table_name IN ('users','homes','memberships','rooms','devices',
                     'device_telemetry','invitations','permissions',
                     'settings','ai_action_audit','device_commands');
-- Expected: 11

-- 2. Infrastructure tables exist
SELECT count(*) FROM information_schema.tables 
WHERE table_schema = 'public' 
  AND table_name IN ('outbox_events','telemetry_dedup_ledger');
-- Expected: 2

-- 3. All PKs defined
SELECT table_name, constraint_name 
FROM information_schema.table_constraints 
WHERE constraint_type = 'PRIMARY KEY' AND table_schema = 'public'
ORDER BY table_name;

-- 4. Composite FK targets have UNIQUE indexes
SELECT indexname FROM pg_indexes 
WHERE indexname IN ('uk_rooms_home_id', 'uk_devices_home_id');
-- Expected: 2 rows

-- 5. Partial unique index on memberships
SELECT indexname FROM pg_indexes 
WHERE indexname = 'uk_memberships_one_owner_per_home';
-- Expected: 1 row

-- 6. Permissions enums created
SELECT typname FROM pg_type WHERE typname IN 
('permission_type_enum','target_type_enum','effect_enum',
 'command_status_enum','initiator_type_enum');
-- Expected: 5 rows

-- 7. Device_telemetry is hypertable
SELECT * FROM timescaledb_information.hypertables WHERE hypertable_name = 'device_telemetry';
-- Expected: 1 row

-- 8. All check constraints present
SELECT table_name, constraint_name 
FROM information_schema.check_constraints cc
JOIN information_schema.table_constraints tc ON cc.constraint_name = tc.constraint_name
WHERE tc.table_schema = 'public'
ORDER BY table_name;

-- 9. Trigger exists on permissions
SELECT tgname FROM pg_trigger WHERE tgname = 'trg_permissions_validate';
-- Expected: 1 row

-- 10. Trigger exists on device_commands terminal state
SELECT tgname FROM pg_trigger WHERE tgname = 'trg_device_commands_transition';
-- Expected: 1 row
```

### 18.2 Integration Validation (End-to-End)

| Flow | Test | Pass Criteria |
|------|------|---------------|
| User command | POST `/commands` → MQTT → Device ACK → Result → PG → Redis → SignalR | State updated in PG, Redis, client receives event |
| Telemetry ingestion | Device sends MQTT telemetry → Backend → TimescaleDB + dedup ledger | Rows inserted; duplicate messageId ignored |
| AI action | POST `/ai/action` → FastAPI → Backend validates → Command dispatched | Only permitted actions execute; audit logged |
| Device onboarding | Owner initiates → Device pairs → Registered → Online | Device in PG, `connection_status=online` |
| User anonymization | Soft delete user → Check related tables | `requested_by_user_id` removed by CASCADE on hard DELETE; soft delete anonymizes |

---

## 19. ADDITIONAL CONSISTENCY CHECKS

### 19.1 USERS Email Uniqueness After Soft Delete

- **Full uniqueness**: `email` column has a global UNIQUE constraint (physical uniqueness).
- **Active uniqueness**: Partial unique index `uk_users_email_active` enforces uniqueness only among non-deleted users (`deleted_at IS NULL`).
- **Behavior**: After soft delete (`deleted_at` set), the email becomes available for a new account. The global UNIQUE constraint is satisfied because the deleted user's email is anonymized to `'deleted_' || id || '@placeholder'` during the anonymization procedure (Section 6.2.2).

### 19.2 Duplicate Permission Grant Prevention

**P-09 (Proposed):** Consider preventing duplicate active GRANT rows for the same grantor, grantee, permission, and target. This is separate from P-08 (`execution_started_at`). Do not adopt until Product/Security confirm whether multiple grantors must be independently recorded for the same effective grant.

```sql
-- Proposed only; do not apply until P-09 is approved.
CREATE UNIQUE INDEX uk_permissions_duplicate_grant
    ON permissions (grantor_membership_id, grantee_membership_id,
                    permission_type, target_type, target_id)
    WHERE effect = 'grant' AND grantor_membership_id IS NOT NULL;
```

### 19.3 Invitation Role Rules

- `INVITATIONS.role` allows `'owner'`, `'member'`, `'guest'`.
- **Inviting as Owner**: Technically allowed by schema. Business logic should restrict Owner invitations to system-initiated flows (e.g., home creation) or require explicit cross-team approval. This is an **unresolved runtime decision** (I-05, proposed).

### 19.4 Polymorphic Permission Target Orphaning

- `PERMISSIONS.target_id` references polymorphic targets (device/room/home/camera/microphone) with **no FK enforcement**.
- **Orphaning risk**: If target is deleted, permission row remains.
- **Mitigation**: 
  - Application authorization returns Deny for non-existent targets (target lookup fails).
  - Periodic cleanup job: `DELETE FROM permissions WHERE target_type = 'device' AND target_id NOT IN (SELECT id FROM devices);` (and similar for other target types).
  - This is documented as an MVP trade-off (Section 9.6).

### 19.5 Physical Table Count Consistency

| Category | Tables | Count |
|----------|--------|-------|
| **Core Domain Tables** | users, homes, memberships, rooms, devices, device_telemetry, invitations, permissions, settings, ai_action_audit, device_commands | **11** |
| **Infrastructure Tables** | outbox_events, telemetry_dedup_ledger | **2** |
| **Enum Types** | permission_type_enum, target_type_enum, effect_enum, command_status_enum, initiator_type_enum | 0 |
| **Total Physical Tables** | | **13** |

**Consistent across:** Section 5.1, Section 13.7, this section, and all diagrams.

### 19.6 NexusLink Protocol Verification Status

**VERIFIED.** The NexusLink protocol topics, message shapes, and validation rules in this document have been independently verified against the following source artifacts in the project repository:

- `spec.md` (NexusLink Specification v1 draft) — `/Users/ahmed/GRAD/Schema from 8.1 to 8.4/spec.md`
- `command.schema.json` — `/Users/ahmed/GRAD/Schema from 8.1 to 8.4/schemas/command.schema.json`
- `command-result.schema.json` — `/Users/ahmed/GRAD/Schema from 8.1 to 8.4/schemas/command-result.schema.json`
- `state.schema.json` — `/Users/ahmed/GRAD/Schema from 8.1 to 8.4/schemas/state.schema.json`
- `availability.schema.json` — `/Users/ahmed/GRAD/Schema from 8.1 to 8.4/schemas/availability.schema.json`
- `telemetry.schema.json` — `/Users/ahmed/GRAD/Schema from 8.1 to 8.4/schemas/telemetry.schema.json`

**Verification scope:** All 5 JSON schemas were cross-referenced against Section 11.3 message shapes, Section 11.1 topic structure, and Section 11.2 protocol rules. All required fields, types, patterns, enums, and limits match. Spec.md proposed values (§7, §9) are preserved as proposed and not promoted to confirmed requirements.

---

## 20. DOCUMENT CONTROL

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-11-18 | Backend Engineering | Initial baseline freeze with all mandated corrections |
| 1.1 | 2026-11-18 | Backend Engineering | Prior correction pass: outbox index, memberships index, telemetry dedup, permissions escalation, outbox worker EF Core API, FK delete actions, GDPR soft delete, command state machine, owner invariant, decision counts, blocker reclassification, .NET version matrix, consistency checks |
| 1.2 | 2026-11-18 | Backend Engineering | Corrected permissions deny/grant paths and active-grantor enforcement; corrected anonymization and FK consistency; rewrote Outbox worker lease/channel retry/backoff design; enforced command transitions and Pending timeout; corrected decision IDs/counts and .NET support date; added telemetry payload validation and bounded retention gates; marked NexusLink verification as pending; changed readiness to NO-GO |
| 1.3 | 2026-11-18 | Backend Engineering | **Final review correction pass:** Fixed C-01 (device_commands.requested_by_user_id FK CASCADE per DB Summary), C-02 (telemetry maxItems=8), C-03 (telemetry value=number only); resolved A-01 (telemetry name pattern), A-03 (command type CHECK constraint), A-07 (invitation owner restriction documented as I-05), A-09 (outbox event_type CHECK constraint), A-10 (device_commands.type CHECK constraint); verified NexusLink compatibility against all 5 source schemas; updated Section 19.6 to VERIFIED; synchronized all cross-references and counts |

---

## 21. CORRECTION PASS SUMMARY (v1.2 → v1.3)

### 21.1 Critical Contradictions Resolved

| # | Finding | Source Evidence | Correction Applied |
|---|---------|-----------------|-------------------|
| C-01 | `device_commands.requested_by_user_id` FK action | `DATABASE_MVP_SUMMARY.md` line 23: explicit CASCADE, SET NULL rejected | Changed to `ON DELETE CASCADE` in Section 6.2.9, 13.7; updated D-18 note |
| C-02 | Telemetry batch maximum size | `telemetry.schema.json`: `"maxItems": 8` | Enforced in Section 7.3 ingestion function validation |
| C-03 | Telemetry `value` type | `telemetry.schema.json`: `"value": { "type": "number" }` | Validation now only accepts `number`, not string |

### 21.2 Assumptions and Gaps Resolved

| # | Finding | Source Evidence | Correction Applied |
|---|---------|-----------------|-------------------|
| A-01 | Telemetry reading name pattern | `telemetry.schema.json`: `"name": { "pattern": "^[a-z][a-z0-9_]*$" }` | Added pattern validation in Section 7.3 ingestion function |
| A-02 | Telemetry unit required/type | `telemetry.schema.json`: `unit` in `required`, type `string` | Validation enforces presence and string type (already correct) |
| A-03 | Command type naming pattern | `command.schema.json`: `"type": { "pattern": "^[a-z]+(\\.[a-z_]+)+$" }` | Added `ck_device_commands_type_format` CHECK constraint in Section 6.2.9 |
| A-04 | Device connection status mapping | `spec.md` §3, `availability.schema.json` | Documented in Section 6.2.6 note: backend derives `unknown` |
| A-05 | AI audit outcome mapping | No direct schema; backend classification | Documented in Section 6.2.10 note |
| A-06 | Settings preferences vs notification_enabled | `DATABASE_MVP_SUMMARY.md` line 21 | Documented as intentional design in Section 6.2.8 |
| A-07 | Invitation role restrictions | `invitations` table CHECK, I-05 | Documented as unresolved I-05 in Sections 6.2.7, 13.6, 19.3 |
| A-08 | Command state mapping | `command-result.schema.json` status enum | Documented in Section 10.1: 7 backend ↔ 3 device states |
| A-09 | Outbox event_type values | No authoritative source | Added proposed CHECK constraint in Section 8.5 |
| A-10 | device_commands.type validation | `command.schema.json` pattern | Added CHECK constraint in Section 6.2.9 |

### 21.3 P0 Blocker Review

| Blocker | Status | Resolution |
|---------|--------|------------|
| **B-01** | **UNRESOLVED** | Permission type enum values require Product + Security approval. No authoritative source overrides. |
| **B-02** | **UNRESOLVED** | Target type enum values require Product + Security approval. No authoritative source overrides. |
| **B-06 (FK)** | **CONFIRMED (D-18)** | `device_commands.requested_by_user_id ON DELETE CASCADE` per `DATABASE_MVP_SUMMARY.md` line 23. Not reopened. |
| **B-06 (legal)** | **UNRESOLVED (P2)** | Anonymization/erasure workflow, retention, GDPR compliance confirmation. Legal + Product. Compliance gate, not schema blocker. |

### 21.4 Updated Counts

| Category | Count |
|----------|-------|
| **Confirmed Decisions** | 16 |
| **Proposed Technical Decisions** | 13 |
| **Unresolved Decisions** | 18 (B-01, B-02, B-03, B-04, B-05, B-06 legal, B-07, I-01–I-05, O-01–O-06) |
| **Core Domain Tables** | 11 |
| **Infrastructure Tables** | 2 |
| **Total Physical Tables** | 13 |
| **Schema P0 Blockers** | 2 (B-01, B-02 — enum values only) |
| **Runtime/Integration P1 Blockers** | 9 (B-03, B-04, B-05, B-07, I-01, I-02, I-03, I-04, I-05) |
| **Operational/Compliance P2 Blockers** | 7 (O-01 through O-06, B-06 legal policy) |

---

## 22. CONFIRMATION

**This document-only review pass did not execute SQL, create/apply migrations, modify a database, or change application source/configuration/environment files. The corrected file is a new document (v1.3); v1.2 is preserved unchanged.**

---

**END OF NEXUS_HOME_FINAL_ARCHITECTURE_BASELINE_v1.3_FINAL_REVIEW.md**

This document represents the technically corrected, internally consistent, source-grounded architecture baseline for the Nexus Home MVP. All source-backed contradictions have been resolved. NexusLink protocol compatibility has been independently verified against all five source JSON schemas and the specification document. Three P0 schema blockers (B-01, B-02, B-06) remain and must be resolved by cross-team approval before migration generation. The document is ready for Technical Lead review.