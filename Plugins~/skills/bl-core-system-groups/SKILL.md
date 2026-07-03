---
name: bl-core-system-groups
description: "Use for placing BovineLabs Core systems in scene, simulation, transform, relevancy, pause-aware, presentation, or initialization groups."
---

# Core System Groups

Resolve Core source from `Packages/com.bovinelabs.core` or its exact package-cache entry. Choose the semantic phase, world scope, and pause policy before adding peer-order attributes.

## Group Selection

| Need | Group |
|---|---|
| Run before `SceneSystemGroup` or before unload removes entities | `BeforeSceneSystemGroup` |
| Consume freshly loaded subscene entities | `AfterSceneSystemGroup` |
| Broad start-of-simulation setup used by later systems | `BeginSimulationSystemGroup` |
| Write `LocalTransform` before transform propagation | `BeforeTransformSystemGroup` |
| Read completed transform results without writing transforms | `AfterTransformSystemGroup` |
| Server-only NetCode ghost relevancy | `RelevancySystemGroup` |
| Custom simulation root disabled during normal pause | derive from `BLSimulationSystemGroup` |

Do not use `BeginSimulationSystemGroup` as a generic “run early” bucket or `RelevancySystemGroup` as a generic network group.

## Ordering And Pause

- Select the group first; then use `UpdateBefore`/`UpdateAfter` only among peers in that phase.
- Ordering that spans phases usually indicates the wrong group.
- `BLSimulationSystemGroup` implements `IDisableWhilePaused`. Mark exceptional child systems with `IUpdateWhilePaused` rather than inventing a new root.
- Check each built-in group's `WorldSystemFilter`; re-evaluate placement when the system needs a different world scope.

## Boundaries

Use `$bl-core-lifecycle` for initialize/destroy sequencing and `$bl-core-pause` for pause-mode behavior. This skill answers where a system belongs.

## Failure Triage

- Needs loaded scene data but runs pre-scene: move toward `AfterSceneSystemGroup`.
- Writes transforms after propagation: move toward `BeforeTransformSystemGroup`.
- A local ordering issue caused a phase move: restore the phase and order peers instead.
- Feature unexpectedly stops while paused: inspect its root group's pause policy.
- Relevancy logic runs in non-server worlds: use `RelevancySystemGroup` and verify filters.

Inspect the selected group source under `BovineLabs.Core.Extensions/Groups` for exact update and world attributes.
