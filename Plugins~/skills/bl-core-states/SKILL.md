---
name: bl-core-states
description: "Use for BovineLabs Core state components, registration, transitions, history variants, enableable states, or state-system debugging."
---

# Core States

Resolve Core source from `Packages/com.bovinelabs.core` or its exact package-cache entry. Select the model from required behavior before writing transition systems.

## Model Selection

| Requirement | Model | Component shape |
|---|---|---|
| Exactly one active state | `StateModel` | Current/previous `byte` |
| Multiple coexisting states | `StateFlagModel` | Current/previous bit array, commonly `BitArray256` |
| Exclusive state with back/forward history | `StateModelWithHistory` | Byte state plus history components |
| Flag set with history | `StateFlagModelWithHistory` | Bit arrays plus history components |
| Stable component presence with enabled/disabled state | `StateModelEnableable` | Every registered state component implements `IEnableableComponent` |

Do not layer custom history beside a non-history model or mix byte and flag component shapes.

## Registration

- `StateAPI.Register` maps one system handle to one state instance component and key; a system may register only one state instance.
- Registration adds `RequireForUpdate` by default. Set `queryDependency: false` only when the system must update without the registered component.
- Register by byte key or by string through the existing `KSettingsBase` key source; avoid unnecessary key indirection.

## System Lifecycle

1. Store the model in an `ISystemStartStop` system.
2. Construct it in `OnStartRunning`.
3. Run or schedule its update in `OnUpdate` using the cheapest mode compatible with surrounding work.
4. Dispose it in `OnStopRunning`.

Do not recreate a model every frame or force main-thread execution without a real dependency.

## Enableable Variant

`StateModelEnableable` toggles enabled state instead of adding/removing components. Use it when stable archetypes and enableable queries are intentional; downstream systems must query enablement correctly.

## Failure Triage

- System never updates: inspect the registration's `RequireForUpdate` behavior and component presence.
- Multiple states should coexist but one survives: use a flag model.
- Exclusive state leaves several active components: verify a flag model was not selected accidentally.
- Enableable model asserts: every registered state component must be enableable.
- Hand-built back/forward tracking appears: switch to the matching history model.

For exact behavior read `Documentation~/States.md`, `StateAPI.cs`, and the selected model source under `BovineLabs.Core/States`.
