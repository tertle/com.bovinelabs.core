---
name: bl-core-lifecycle
description: "Use when designing, implementing, extending, refactoring, or debugging lifecycle initialization/destruction flows in com.bovinelabs.core, including InitializeEntity, InitializeSubSceneEntity, DestroyEntity, lifecycle authoring, and lifecycle system-group ordering."
---

# Core Lifecycle Usage

Use this skill for entity lifecycle behavior in core extensions: initialization, destruction, and phase-ordered command buffering.
Resolve core package paths against `Packages/com.bovinelabs.core` or the matching `Library/PackageCache/com.bovinelabs.core@*`.

## Workflow

1. Identify whether the change is initialization flow, destruction flow, or authoring setup.
2. Read `references/lifecycle.md`.
3. Follow the setup and ordering rules for lifecycle groups/components before writing systems.
4. Prefer lifecycle-driven destruction (`DestroyEntity` enable) over direct destruction in gameplay systems.

## Routing

- `references/lifecycle.md`: `LifeCycleAuthoring`, initialize/destroy components, lifecycle system groups, destruction propagation, timer-based destruction, and command buffer usage across lifecycle phases.
