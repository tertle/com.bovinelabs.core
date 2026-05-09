# Core States Reference

## Read First

Use these sources when the task needs exact behavior:

- `Documentation~/States.md`
- `BovineLabs.Core/States/StateAPI.cs`
- `BovineLabs.Core/States/StateModel.cs`
- `BovineLabs.Core/States/StateFlagModel.cs`
- `BovineLabs.Core/States/StateModelWithHistory.cs`
- `BovineLabs.Core/States/StateFlagModelWithHistory.cs`
- `BovineLabs.Core/States/StateModelEnableable.cs`
- `BovineLabs.Core/States/StateInstance.cs`

## Model Selection

Choose the model from the behavior you need, not from naming preference.

### `StateModel`

Use when only one state may be active at a time.

Typical fit:

- high-level app state
- camera mode
- UI mode where states are mutually exclusive

The state component value is a `byte`.

### `StateFlagModel`

Use when multiple states may be active at once.

Typical fit:

- overlapping flags
- UI panels or conditions that can coexist
- state sets that should not force mutual exclusion

The state component value is a bit array, commonly `BitArray256`.

### `StateModelWithHistory`

Use when the state system must track back and forward history in addition to the current and previous state.

Do not pick this model unless navigation or history behavior is a real requirement.

### `StateFlagModelWithHistory`

Use when the state set is flag-based and also needs history.

This is the most complex option. Use it only when both properties are required.

### `StateModelEnableable`

Use when the active state should toggle `IEnableableComponent` state components instead of adding and removing components.

All registered state instance components must be enableable for this model.

## Component Shape

State systems need at least:

- a current state component
- a previous state component

Single-state models use `byte`.
Flag-state models use a bit array.

History variants also need back and forward history components.

Do not mix byte-based and flag-based component shapes with the wrong model.

## `StateAPI.Register`

`StateAPI.Register` wires a system handle to a specific state instance component and state key.

Two important details:

- each system may only register one state instance
- by default it adds `RequireForUpdate` for the registered state instance component

That second point matters a lot:

- default behavior is usually correct for normal state-specific systems
- set `queryDependency` to `false` only when the system must not be gated that way

If a state system unexpectedly never updates, inspect `queryDependency` before changing ordering or model code.

## Registration Choices

There are two common registration styles:

- register by byte state key directly
- register by string through the `KSettingsBase` overload

Choose the form that matches the existing key source for the feature. Do not introduce an unnecessary key indirection.

## State System Pattern

The state model struct is usually held by a system implementing `ISystemStartStop`.

Typical lifecycle:

1. construct the model in `OnStartRunning`
2. dispose it in `OnStopRunning`
3. run or schedule the model update in `OnUpdate`

Do not recreate the model every frame.

## Update Mode Choices

The state models expose different execution modes such as:

- immediate run
- scheduled update
- parallel scheduled update where supported

Choose the cheapest mode that matches the surrounding system.

Do not force main-thread execution unless the model or surrounding logic truly needs it.

## Enableable-State Rules

`StateModelEnableable` has different constraints from the add/remove models:

- registered state instance components must be enableable
- it toggles enabled state rather than adding/removing instance components

Use it when stable component presence matters or when enableable semantics are a better fit than structural changes.

Do not switch to the enableable model casually; it changes how downstream systems should query and reason about state components.

## Common Patterns

### Mutually Exclusive App State

- use `StateModel`
- store current and previous as `byte`
- register one component per state

### Coexisting Feature Flags

- use `StateFlagModel`
- store current and previous as a bit array
- toggle bits without clearing unrelated flags

### Navigation Or Back/Forward History

- use one of the history variants
- add the extra history components explicitly

### Stable Components With On/Off Semantics

- use `StateModelEnableable`
- ensure all state components are `IEnableableComponent`

## Failure Checklist

- State system never updates:
  check whether `StateAPI.Register` added `RequireForUpdate` and whether that was intended.
- Multiple states should coexist but only one survives:
  use a flag model instead of `StateModel`.
- A supposedly exclusive state system leaves multiple state components active:
  verify the feature is not using a flag model accidentally.
- Enableable model logs or asserts about state components:
  check that every registered state component is enableable.
- History behavior is being added manually beside a non-history model:
  switch to the appropriate history variant instead of layering custom tracking on the side.
