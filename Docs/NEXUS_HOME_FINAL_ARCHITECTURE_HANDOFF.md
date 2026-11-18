# NEXUS HOME — FINAL ARCHITECTURE HANDOFF

**Canonical Document:** `NEXUS_HOME_FINAL_ARCHITECTURE_BASELINE_v1.3_FINAL_REVIEW.md` (v1.3)  
**Review Report:** `NEXUS_HOME_v1.3_FINAL_REVIEW_REPORT.md`  
**Date:** 2026-11-18  
**Status:** Architecture investigation cycle complete — no further versions will be created

---

## FINAL READINESS STATUSES

| Gate | Status | Condition |
|------|--------|-----------|
| **Ready for Technical Lead Review** | **YES** | Document is internally consistent, source-grounded, all contradictions resolved |
| **Technically Approved** | **PENDING** | Awaiting cross-team resolution of B-01, B-02, and B-06 legal policy |
| **Migration Generation** | **NO-GO** | Blocked by 2 schema-affecting decisions (B-01, B-02 enum values) |
| **Migration Application** | **NOT AUTHORIZED** | Requires explicit database-change approval after Technical Lead authorization |

---

## CONFIRMED DATABASE DECISIONS (Immutable for MVP)

| ID | Decision | Source |
|----|----------|--------|
| **D-01** | 11 domain tables for MVP | Task Instructions, DB Summary, ERD |
| **D-02** | DEVICE_COMMANDS included in MVP | Technical Lead override |
| **D-03** | No automation tables | All sources consistent |
| **D-04** | pgvector excluded from MVP | Backend Scope, Contract |
| **D-05** | PostgreSQL 16+ primary DB | Backend Scope, DB Summary |
| **D-06** | TimescaleDB for telemetry | Backend Scope, DB Summary |
| **D-07** | Redis for live state/cache | Backend Scope, Contract |
| **D-12** | NexusLink over MQTT/TLS | spec.md, Contract |
| **D-13** | Device external_id globally unique | DB Summary line 16 |
| **D-14** | Device soft delete (removed_at) | DB Summary line 19 |
| **D-15** | Composite FK devices→rooms MATCH SIMPLE | DB Summary line 14 |
| **D-16** | RESTRICT on home→devices delete | DB Summary line 19 |
| **D-17** | RESTRICT on device→telemetry/audit/commands physical delete; normal retirement uses soft delete | DB Summary line 19 |
| **D-18** | **CASCADE on user→device_commands.requested_by_user_id** | DB Summary line 23 |
| **D-19** | Permissions effect grant/deny | DB Summary line 20 |
| **D-20** | Settings UK on user_id (personal only) | DB Summary line 21 |

> **D-18 Note:** The FK action `ON DELETE CASCADE` for `device_commands.requested_by_user_id` is **Confirmed** per `DATABASE_MVP_SUMMARY.md` line 23. This decision is not reopened. Alternatives (`RESTRICT`, `SET NULL`) were explicitly considered and rejected.

---

## EXACT OPEN DECISIONS AND RESPONSIBLE TEAMS

### P0 — Schema Blockers (Block Migration Generation)

| ID | Decision | Exact Choices Required | Responsible |
|----|----------|------------------------|-------------|
| **B-01** | Permission type enum values | Approve the exact enum values for `permission_type_enum` (currently proposed: `device:control`, `device:view`, `device:manage`, `home:manage`, `member:manage`, `camera:stream`, `camera:view`, `microphone:stream`, `settings:manage`, `ai:propose`, `ai:execute`) | **Product + Security** |
| **B-02** | Target type enum values | Approve the exact enum values for `target_type_enum` (currently proposed: `device`, `home`, `room`, `camera`, `microphone`) | **Product + Security** |

### P2 — Compliance/Operational Gate (Does NOT Block Schema/Migration)

| ID | Decision | Exact Choices Required | Responsible |
|----|----------|------------------------|-------------|
| **B-06 (legal)** | User anonymization/erasure workflow | 1. Anonymization procedure details (retention periods, PII fields to scrub)<br>2. Whether CASCADE satisfies GDPR "right to erasure" or soft-delete is primary path<br>3. Erasure workflow approval | **Legal + Product** |

### P1 — Runtime/Integration Blockers (Block Feature Integration)

| ID | Decision | Responsible |
|----|----------|-------------|
| **B-03** | Role default permission matrix | Product + Security |
| **B-04** | Device capabilities JSON schema | Embedded + AI |
| **B-05** | Telemetry retention period | Product + DevOps + Legal |
| **B-07** | MQTT broker selection | Network + DevOps |
| **I-01** | Owner transfer concurrent workflow | Product |
| **I-02** | Late command result handling policy | Backend Lead |
| **I-03** | Redis HA topology | DevOps |
| **I-04** | Camera/microphone stream architecture | AI + Embedded + Network |
| **I-05** | Invitation role rules (Owner invitations) | Product + Security |

### P2 — Operational Blockers (Block Production Hardening)

| ID | Decision | Responsible |
|----|----------|-------------|
| **O-01** | Cloud hosting / managed services | DevOps + Backend |
| **O-02** | Push notification provider | Mobile + DevOps |
| **O-03** | Home-level settings scope | Product |
| **O-04** | Conversation/context storage | AI + Product |
| **O-05** | Audit PII encryption at rest | Security + Legal |
| **O-06** | Telemetry dedup ledger retention period | DevOps + Product |

---

## WHAT BLOCKS MIGRATION GENERATION

**Only B-01 and B-02 (enum values).** These directly affect `CREATE TYPE` statements and `CHECK` constraints in the initial migration.

**What does NOT block migration generation:**
- B-06 legal policy (compliance gate, FK decision already confirmed)
- B-03, B-04, B-05 (runtime/app logic, retention policy applied later)
- B-07 (infrastructure)
- All I-01–I-05, O-01–O-06

---

## WHAT DOES NOT BLOCK DATABASE DESIGN

| Item | Reason |
|------|--------|
| B-03 role defaults | Applied in application logic; table/trigger work without it |
| B-04 device capabilities | `jsonb` column created without CHECK constraint; validation addable later |
| B-05 telemetry retention | Hypertable created without retention; policy added via `add_retention_policy()` later |
| B-06 legal policy | FK decision confirmed; anonymization workflow is compliance/operational |
| B-07 MQTT broker | Infrastructure provisioning; schema independent |
| I-01 through I-05 | Runtime/integration decisions |
| O-01 through O-06 | Production hardening decisions |

---

## IMMEDIATE NEXT PROJECT PHASE AND PREREQUISITES

### Phase 1: Foundation (Weeks 1-2)

**Prerequisites:**
1. **Product + Security approve B-01 and B-02** (enum values)
2. **Technical Lead reviews v1.3 document** and authorizes migration generation

**Deliverables:**
- REST API contract / OpenAPI
- Database schema / ERD (this document, with approved enum values)
- Project setup (Backend solution skeleton)
- Development environments documented
- Dockerized local environment (PG + TimescaleDB + Redis)

**Note:** B-06 legal policy does not block Phase 1 schema generation. It is a compliance approval gate required before production.

---

## NEXUSLINK PROTOCOL STATUS

**VERIFIED** against all five source JSON schemas and spec.md:
- `command.schema.json`
- `command-result.schema.json`
- `state.schema.json`
- `availability.schema.json`
- `telemetry.schema.json`
- `spec.md` (NexusLink Specification v1 draft)

Embedded/Network/DevOps can proceed with protocol implementation using v1.3 as reference. Infrastructure provisioning blocked on B-07.

---

## CONFIRMATION

This handoff document was created as the single required deliverable per the architecture freeze directive. The v1.3 baseline and review report have been updated to fix all readiness terminology contradictions and the B-06 classification. No v1.4 will be created. No further architecture audit will be performed.