# NexusLink Specification v1 (draft)

Implements the Nexus Home Technical Integration Contract v1. This document defines the MQTT profile: topics, delivery rules, sessions, validation and security. Message shapes are the JSON Schemas in `schemas/`.

## 1. Topics

Pattern: `nexushome/v1/homes/{homeId}/devices/{deviceId}/{channel}`
`homeId` and `deviceId` are lowercase UUIDs.

| Channel | Message type | Direction | QoS | Retained |
|---|---|---|---|---|
| commands | command | Backend to device | 1 | no |
| command-results | command.result | device to Backend | 1 | no |
| state | state | device to Backend | 1 | yes |
| telemetry | telemetry | device to Backend | 0 | no |
| availability | availability | device and broker (Last Will) to Backend | 1 | yes |

Rules:
- One message type per channel.
- Commands are never retained, so a stale command cannot run later.
- State and availability are retained so a new subscriber gets the latest value at once. A retained value may be old, so the receiver checks `reportedAt` for freshness.
- QoS 1 can deliver a message twice, so receivers must tolerate duplicates.
- The Backend subscribes with wildcards, for example `nexushome/v1/homes/+/devices/+/state`.

## 2. Connection and sessions

- Transport: TLS 1.2 or higher on port 8883. No plaintext port.
- `clientId` and username are the `deviceId`. Each device has its own password.
- Devices connect with a clean session, so the broker never queues commands for an offline device. The Backend marks an unanswered command as TimedOut.
- Keep-alive is 30 seconds.
- Reconnect with exponential backoff and jitter (1 second doubling up to 60 seconds).
- A device synchronizes time with NTP before connecting. Without valid time it must not execute commands.

## 3. Availability and Last Will

- On connect the device registers a Last Will: topic `availability`, QoS 1, retained, payload is an availability message with status `offline`.
- After connecting it publishes status `online` (retained).
- Before a planned disconnect or sleep it publishes `offline` itself.
- Heartbeat: the device republishes `online` every 30 seconds (configurable). The Backend treats a device as offline or unknown after 3 missed intervals.
- `unknown` is derived by the Backend and is never sent by a device.
- The Will payload is fixed at connect time, so its `reportedAt` is the connect time. The Backend uses its own receive time for offline events.

## 4. Command flow

1. The Backend publishes a command on `commands` (QoS 1).
2. The device validates it (section 5). If valid, it publishes `command.result` with status `Acknowledged`.
3. The device executes, then publishes `Succeeded` or `Failed` with `reportedState` or `error`.
4. The device publishes its new `state` (retained).

Rules:
- Acknowledged does not mean the action succeeded.
- A device sends only Acknowledged, Succeeded and Failed. Pending, Sent, TimedOut and Cancelled belong to the Backend.
- The Backend updates state only from `state` messages or `reportedState` in results, never from the requested value.
- The device remembers the last 20 `commandId` values with their results. A repeated `commandId` gets the stored result again and is not executed twice.
- Commands set an explicit target (set temperature to 24), not a toggle.

## 5. Receiver validation (every message, in this order)

1. Valid JSON and at most 16 KB.
2. `schemaVersion` is supported. Otherwise drop the message and log it, with no reply.
3. `messageType` matches the channel.
4. The message matches its JSON Schema.
5. `homeId` and `deviceId` equal the topic, and on a device they equal its own ID. A topic alone is never proof of identity.
6. A `messageId` already seen is dropped.
7. Time:
   - Command: reject if now is later than `expiresAt` (5 seconds tolerance).
   - Live (non-retained) message from a device: reject if `reportedAt` is more than 30 seconds from now.
   - Retained messages are exempt from the 30 second window. They are old by design, so the receiver uses `reportedAt` to show how fresh they are.
8. The command type is supported and its parameters are valid. Otherwise answer `Failed` with an error code.

Device error codes: `UNSUPPORTED_COMMAND`, `INVALID_PARAMETER`, `COMMAND_EXPIRED`, `CLOCK_NOT_SYNCED`, `DEVICE_BUSY`, `EXECUTION_FAILED`.

## 6. Security

- Each device may publish only on its own `deviceId` (`state`, `telemetry`, `availability`, `command-results`) and read only its own `commands`. The Backend reads everything and publishes `commands` only. These rules are enforced by the broker ACL using the username.
- The broker ACL cannot inspect message content, so the receiver checks IDs in the payload (section 5, step 5).
- A device credential is issued at pairing. On removal the credential is deleted and the device is disconnected.
- Devices hold only the CA certificate and must verify the broker certificate.
- Private keys never leave the machine that owns them.

## 7. Limits (proposed)

- Message size: 16 KB (broker `message_size_limit`).
- Telemetry: at most 8 readings per message and 1 message per second per device.
- Keep-alive and heartbeat: 30 seconds.
- Command lifetime: set by the Backend in `expiresAt` (example: 15 seconds).
- Clock tolerance: 5 seconds.

## 8. Versioning

`schemaVersion` is in every message and `v1` is in the topic. A breaking change gets a new version number and a new topic prefix.

## 9. Open points

1. Schemas reject unknown fields, while the contract prefers additive optional fields for compatibility. Confirm with the Backend how new fields are released.
2. Clean sessions for devices versus persistent sessions.
3. Heartbeat interval and offline timeout values.
4. Where the Backend runs (Hub or Cloud) and how it reaches the broker.
5. Pairing and onboarding, credential delivery, and proof of device possession.
6. Extensions outside v1: capabilities announcement and chunked transfer of large data.

## 10. Basis of each rule

Stated in the contract: topic pattern and channel names; command and command.result envelopes; TLS and authenticated connections; per-identity topic ACLs; a device never trusts the topic alone; commands are not retained and carry an expiry; Acknowledged is separate from execution; backoff reconnect; offline after a heartbeat timeout; unknown versions rejected and logged; telemetry fields; UTC timestamps; explicit set commands.

Left to this specification by the contract: QoS per channel, retained flags, session behavior, Last Will use, receiver validation order, duplicate handling.

Proposed values and shapes (need confirmation): state, telemetry and availability message shapes; all numeric limits (keep-alive, heartbeat, backoff, clock tolerance, 30 second window, 16 KB, rate limit, 20 stored command IDs); device error code names; clean sessions; deviceId as clientId and username; the split of status values between device and Backend.
