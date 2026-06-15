# SiteBroker — Machine Data & Counter Specification (Proposal / Draft v0.1)

> This document extends the SiteBroker interface with a **machine data** channel — the publishing of machine **states** and **counter values** to the outside world (MES, productionManager, dashboards, IoT/cloud).
>
> It is the intended **single source of truth** for the data channel, in the same way [Interface & Method Specification](Interface_Specification.md) is for command & control. It deliberately **reuses** the SiteBroker transport (MQTT, retained messages, `MachineNumber`) and links back to the main specification for everything shared; it only specifies what is **new** for the data channel.

---

## Table of contents

1. [Scope & design principles](#1-scope--design-principles)
2. [Relationship to the command interface](#2-relationship-to-the-command-interface)
3. [Topic structure](#3-topic-structure)
4. [Payload format](#4-payload-format)
5. [Machine state](#5-machine-state)
6. [Defined machine data](#6-defined-machine-data)
7. [Defined batch-variant data](#7-defined-batch-variant-data)
8. [Proposed .NET client API](#8-proposed-net-client-api)

---

## 1. Scope & design principles

This channel covers **read-only, machine-originated or client-originated data**: each status or counter value a production client, such as a WEINMANN machine *provides to the outside world*. It is the inverse direction of the command interface and is **publish-only** from the machine's point of view.

The model is intentionally minimal for a first step:

| Principle | Decision |
|-----------|----------|
| **One signal = one topic = one event** | Every value is its own retained MQTT message. |
| **Event on change** | A value is published whenever it changes. |
| **State always readable via retain** | Because every message is **retained**, a newly connected consumer immediately gets the last value of every signal — without any snapshot topic. |
| **No history** | Data carries only the *current* value of a signal. Historical series / alarm history are **out of scope**. |
| **Reuse the existing bus** | Same broker, same `MachineNumber`, same retained-message philosophy as [Interface & Method Specification §1](Interface_Specification.md#1-communication-protocol-mqtt). |

> Like the command interface, data over MQTT carries **lightweight values only**.

---

## 2. Relationship to the command interface

| Aspect | Command interface ([spec](Interface_Specification.md)) | Data interface (this document) |
|--------|--------------------------------------------------------|-------------------------------------|
| Direction | Bidirectional (request → response) | Unidirectional (client → consumers) |
| Topic shape | `{Sender}/{Receiver}/{Resource}/{Id}/{Action}` | `{MachineNumber}/data/{Category}/{Signal}` (single value), `{MachineNumber}/data/{Category}/{Group}/{Key}` (indexed) or `{MachineNumber}/data/{Category}/{Group}/{Key}/{Property}` (indexed entry with several properties) |
| Correlation id | Yes (4th topic segment) | None — data is state, not a transaction |
| Receiver | A named `orchestrator` | A fixed `data` namespace (broadcast) |
| Payload | `GenericPayloadDto` (`Status` + `AdditionalProperties`) | Single-value envelope  |
| Lifecycle / clear | Retained + explicit empty-payload clear | Retained latest value; single-value signals are never cleared, indexed entries are removed with an empty retained payload ([§6](#6-defined-machine-data)) |

---

## 3. Topic structure

Every signal is published to its own topic. There are three topic forms — a **single-value signal**, an **indexed signal group** that can hold several concurrent entries, and an indexed entry that exposes **several properties** (see [§6](#6-defined-machine-data)):

```
{MachineNumber}/data/{Category}/{Signal}                   # single-value signal
{MachineNumber}/data/{Category}/{Group}/{Key}              # indexed signal group (one value per entry)
{MachineNumber}/data/{Category}/{Group}/{Key}/{Property}   # indexed entry with several properties
```

| Signal | Topic |
|--------|-------|
| Machine state | `{MachineNumber}/data/machine/state` |
| Parts counter | `{MachineNumber}/data/machine/parts` |
| Cycles counter | `{MachineNumber}/data/machine/cycles` |
| Meter counter | `{MachineNumber}/data/machine/meter` |
| Program | `{MachineNumber}/data/machine/program` |

**Placeholders**
- `{MachineNumber}` — the same site-wide unique machine id used by the command interface (e.g. `HAP-01`, `M000000001`).
- `{Category}` — the data category for further filtering
- `{Signal}` — one signal name, lower-case (e.g. `state`, `parts`, `cycles`, `meter`).
- `{Group}` — an indexed signal group (`error`, `warning`, `maintenance`, `action`, `storage`); see [§6](#6-defined-machine-data).
- `{Key}` — identifies the individual entry within an indexed group (e.g. an error code).
- `{Property}` — a named property of an indexed entry that exposes more than one value (e.g. a storage's `current-amount`); see [§6](#6-defined-machine-data).

All data messages are published with **retain = true**, so the broker keeps the latest value per topic and delivers it to any new subscriber.

**Defined categories**

| Category | Description |
|--------|-------|
| machine | Machine-related data that are order-neutral. A client of a different type can also be regarded as a machine. |
| batch-variant | Order-related data |

**Subscription examples (consumer side)**
- One machine, everything: `M000000001/data/+/#`
- All machines, only state: `+/data/+/state`
- All machines, everything: `+/data/+/#`

---

## 4. Payload format

All data payloads use the same minimal **single-value envelope** (JSON):

```jsonc
{
  "type": "string",                                        // The value type (number or string)
  "value": "Lore ipsum",                                   // the signal value (string)
  "timestampUtc": "2026-06-14T10:22:33.512Z",              // ISO 8601, machine-side capture time
  "batchVariantId": "770e8400-e29b-41d4-a716-446655440002" // The guid of the batch variant
}
```

| Field | Type | Required | Description |
|-------|------|:--------:|-------------|
| `value` | string | ✅ | The current value of the signal addressed by the topic. |
| `type` | string | ✅ | The current type of the signal addressed by the topic. |
| `timestampUtc` | string (ISO 8601, UTC) | ✅ | When the value was captured **on the machine**. |
| `batchVariantId` | string |  | Execution ID. Used when data is related to a batch variant. |

> **Clearing / removal.** A topic is cleared by publishing an **empty** payload (`""`) instead of the envelope above (see [§6](#6-defined-machine-data) and [Interface & Method Specification §7](Interface_Specification.md#7-clear-mechanism-retained-messages)). A consumer must treat an empty retained payload as *"signal/entry removed"*, not as a value.


---

## 5. Machine state

**Topic:** `{MachineNumber}/data/machine/state` — published **on change only** (a new state implicitly replaces the previous one, exactly like the MMR interface).

```jsonc
{
  "value": "3",                                // see enum below — MMR-compatible
  "type": "number",
  "timestampUtc": "2026-06-14T10:22:33.512Z"
}
```

### MachineState enum

| Value | Name | Meaning |
|------:|------|---------|
| 1 | `Off` | Machine off / not producing |
| 2 | `Idle` | Powered, waiting (no job running) |
| 3 | `Working` | Production running |
| 4 | `Error` | Fault / stopped on error |
| 5 | `Test` | Test / commissioning mode |

> These five values match the *HOMAG MMR OPC-UA Interface* accepted states exactly, so MMR's existing state-text mapping (e.g. `"Working"="Production"`) applies unchanged.

---

## 6. Defined machine data

WEINMANN/HOMAG machines expose the following data points. Each is its own retained topic.

| Signal | Topic | Type | Meaning |
|--------|-------|------|---------|
| `state` | `{MachineNumber}/data/machine/state` | number | Current machine state (1 Off … 5 Test, see [§5](#5-machine-state)) |
| `parts` | `{MachineNumber}/data/machine/parts` | number | Total parts produced |
| `cycles` | `{MachineNumber}/data/machine/cycles` | number | Total machine cycles |
| `meter` | `{MachineNumber}/data/machine/meter` | number | Total meters processed |
| `program` | `{MachineNumber}/data/machine/program` | string | Identifier of the currently loaded program |

Each signal is published as an **event whenever it changes**, and is **retained**, so the latest value is always available to new subscribers. For the counters (`parts`, `cycles`, `meter`) only the absolute total is published, so there is **no relative/delta counter and no double-counting concern** — a consumer always reads the current total directly.

### Indexed signal groups (error, warning, maintenance, action)

Beyond the single-value signals above, the `machine` category also carries **indexed** signal groups that can hold several concurrent entries. Each entry is addressed by an additional `{key}` segment — and, for groups that expose more than one value per entry (e.g. `storage`), a further `{property}` segment. All of them use the same generic payload envelope ([§4](#4-payload-format)).

| Topic | Group | Meaning |
|-------|-------|---------|
| `{MachineNumber}/data/machine/error/{key}` | error | An error, one topic per error `{key}` (e.g. error code) |
| `{MachineNumber}/data/machine/warning/{key}` | warning | A warning, one topic per warning `{key}` |
| `{MachineNumber}/data/machine/maintenance/{key}` | maintenance | A maintenance cycle, one topic per maintenance `{key}` |
| `{MachineNumber}/data/machine/action/{key}` | action | An action, one topic per action `{key}` |
| `{MachineNumber}/data/machine/storage/{key}/{property}` | storage | A material store, one entry per storage `{key}` with several properties (see below) |

`{key}` identifies the individual entry within the group (e.g. the error code). In the payload ([§4](#4-payload-format)) `value` carries the entry's detail (e.g. the error/warning text, the maintenance-due value or the action description) and `type` its value type. A consumer subscribes to a whole group with the MQTT single-level wildcard `+`, e.g. `{MachineNumber}/data/machine/error/+`. Like every other data topic these are retained.

An entry is **removed** by publishing an **empty** retained payload to its topic — e.g. when an error is resolved, a warning clears, a maintenance cycle is done or an action ends. This is the same empty-payload clear convention the command interface uses ([Interface & Method Specification §7](Interface_Specification.md#7-clear-mechanism-retained-messages)).

Most groups carry a single value per `{key}`. The `storage` group instead exposes several **properties** per entry via an additional `{property}` segment, each property being its own single-value topic ([§4](#4-payload-format)):

| Property topic | Type | Meaning |
|----------------|------|---------|
| `{MachineNumber}/data/machine/storage/{key}/current-amount` | number | Current fill level of the store |
| `{MachineNumber}/data/machine/storage/{key}/max-amount` | number | Maximum capacity of the store |
| `{MachineNumber}/data/machine/storage/{key}/current-material-type` | string | Identifier of the material currently loaded |

A consumer subscribes to all properties of one store with `{MachineNumber}/data/machine/storage/{key}/+`, or to all stores with `{MachineNumber}/data/machine/storage/+/+`.

Because a `storage` entry spans several property topics, the empty-payload removal above applies **per property topic**: removing a store (e.g. when it is unloaded or dismantled) means publishing an empty retained payload to **each** of its property topics (`current-amount`, `max-amount`, `current-material-type`).

---

## 7. Defined batch-variant data

Order-related data for the currently executing batch variant. Each value is its own retained topic under the `batch-variant` category; the variant the values belong to is carried in the payload via `batchVariantId` ([§4](#4-payload-format)).

| Signal | Topic | Type | Meaning |
|--------|-------|------|---------|
| `state` | `{MachineNumber}/data/batch-variant/state` | number | Execution state: 1 Inactive, 2 Active, 3 Done, 4 Aborted (same values as `RunStatus`) |
| `progress` | `{MachineNumber}/data/batch-variant/progress` | number | Progress of the variant in percent (0–100) |
| `meter` | `{MachineNumber}/data/batch-variant/meter` | number | Meters processed for this variant |
| `started` | `{MachineNumber}/data/batch-variant/started` | string | Start timestamp (ISO 8601, UTC) |
| `finished` | `{MachineNumber}/data/batch-variant/finished` | string | End timestamp (ISO 8601, UTC) |

As with all data topics these are published on change and retained. Because the values describe the current variant, `batchVariantId` in the payload tells consumers which execution they belong to.

---

## 8. Proposed .NET client API

To stay symmetric with the existing library ([spec §8](Interface_Specification.md#8-net-client-library-api)), the data channel adds a publisher on the machine side and a consumer on the orchestrator/observer side, registered by the same `AddSiteBroker(...)` call. The machine number is taken from `SiteBrokerOptions`; the publisher sets `retain = true` and stamps `timestampUtc` itself.

### Machine side — `ISiteBrokerDataPublisher`

The topic segments `category`, `signal`, `group` and `property` are typed enums
(`DataCategory`, `DataSignal`, `DataGroup`, `StorageProperty`); each enum member maps to its
lower-case wire segment via a `[TopicSegment]` attribute (e.g. `DataCategory.BatchVariant` → `batch-variant`).
Only `key` stays a free string, because keys are dynamic identifiers (error codes, storage ids).

```csharp
// Single-value signal, e.g. (DataCategory.Machine, DataSignal.State, "3", "number")
Task PublishSignal(DataCategory category, DataSignal signal, string value, string type, Guid? batchVariantId = null);

// One entry of an indexed group, e.g. (DataCategory.Machine, DataGroup.Error, "E-1042", "Vacuum too low", "string")
Task PublishIndexed(DataCategory category, DataGroup group, string key, string value, string type, Guid? batchVariantId = null);

// One property of an indexed entry that exposes several values,
// e.g. (DataCategory.Machine, DataGroup.Storage, "S1", StorageProperty.CurrentAmount, "42", "number")
Task PublishIndexedProperty(DataCategory category, DataGroup group, string key, StorageProperty property, string value, string type, Guid? batchVariantId = null);

// Remove an indexed entry by publishing an empty retained payload (e.g. error resolved)
Task RemoveIndexed(DataCategory category, DataGroup group, string key);

// Remove a single property of an indexed entry (e.g. one storage property);
// pass property = null to remove every known property topic of the entry.
Task RemoveIndexedProperty(DataCategory category, DataGroup group, string key, StorageProperty? property = null);
```

Typed convenience helpers wrap the generic methods, e.g. `PublishMachineState(MachineState)`,
`PublishParts(long)`, `PublishProgram(string)`, `PublishError(key, text)`,
`PublishStorage(key, currentAmount, maxAmount, currentMaterialType)` and the
`PublishBatchVariant*` helpers.

### Consumer side — `ISiteBrokerDataConsumer`

```csharp
// Fires for any signal or indexed entry. An empty retained payload arrives with Removed == true.
event EventHandler<DataEventArgs> DataReceived;
```

`DataEventArgs` carries `MachineNumber`, `Category`, `Name` (the signal or group), `Key`, the optional
`Property`, `Value`, `Type`, `TimestampUtc`, the optional `BatchVariantId` and a `Removed` flag — the
raw wire strings. It additionally exposes the parsed enums `CategoryValue`, `SignalValue`, `GroupValue`
and `PropertyValue` (each `null` for an unknown segment). Topic building extends `TopicHelper` with
`GetDataTopic(machineNumber, category, signal)`, an indexed overload `(…, group, key)` and a property
overload `(…, group, key, property)`.