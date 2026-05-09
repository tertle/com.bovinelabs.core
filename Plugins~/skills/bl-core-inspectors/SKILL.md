---
name: bl-core-inspectors
description: "Use when designing, implementing, extending, refactoring, or debugging custom inspectors and property drawers in com.bovinelabs.core, including ElementEditor/ElementProperty workflows and prefab-aware inspector behavior."
---

# Core Inspectors Usage

Use this skill for inspector and property-drawer workflows in core/editor code.
Resolve core package paths against `Packages/com.bovinelabs.core` or the matching `Library/PackageCache/com.bovinelabs.core@*`.

## Workflow

1. Read `references/inspectors.md`.
2. Follow the read order and build pattern in that reference.
3. Keep state isolated per drawn element (`Cache<T>()`) and preserve prefab-aware behavior where required.

## Routing

- `references/inspectors.md`: `ElementEditor`, `ElementProperty`, `PrefabElementEditor`, `PrefabElementProperty`, and inspector UI behavior.
