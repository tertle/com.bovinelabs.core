---
name: bl-core-subscenes
description: "Use when creating, wiring, extending, or debugging com.bovinelabs.core subscene loading workflows, including SubSceneSettings/SubSceneSet authoring, world targeting/load flags, runtime LoadSubScene/SubSceneLoaded behavior, asset loading, and editor subscene tooling."
---

# Core SubScenes Usage

Use this skill for subscene settings/authoring, runtime load control, asset loading, and editor subscene tooling.
Resolve core package paths against `Packages/com.bovinelabs.core` or the matching `Library/PackageCache/com.bovinelabs.core@*`.

## Workflow

1. Read `references/subscenes.md`.
2. Identify whether the task is authoring setup, runtime load behavior, post-load behavior, or editor tooling.
3. Apply the world-targeting and load-rule guidance before changing systems or assets.
4. If the task also changes initialization or destruction behavior, coordinate with the lifecycle skill after the subscene routing is clear.

## Routing

- `references/subscenes.md`: `SubSceneSettings`, `SubSceneSet`, `AssetSet`, `SubSceneLoadAuthoring`, `LoadSubScene`/`SubSceneLoaded`, post-load command buffers, and editor override/live-baking flows.
