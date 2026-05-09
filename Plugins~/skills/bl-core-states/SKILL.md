---
name: bl-core-states
description: "Use when creating, extending, refactoring, or debugging com.bovinelabs.core state workflows, including StateModel and StateFlagModel variants, StateAPI.Register, state-instance components, history support, and enableable-state behavior."
---

# Core States Usage

Use this skill for app or feature state systems built on `com.bovinelabs.core` state models and registration APIs.
Resolve core package paths against `Packages/com.bovinelabs.core` or the matching `Library/PackageCache/com.bovinelabs.core@*`.

## Workflow

1. Read `references/states.md`.
2. Choose the state model from behavior first: single state, flags, history, or enableable.
3. Define the state and previous-state components before wiring registration systems.
4. Check `StateAPI.Register` dependency behavior before assuming a state system will update.

## Routing

- `references/states.md`: model selection, `StateAPI.Register`, state-instance wiring, `RequireForUpdate` behavior, and the update/run patterns for state systems.
