# Core System Groups Reference

## Read First

Use these sources when the task needs exact behavior:

- `BovineLabs.Core.Extensions/Groups/BeforeSceneSystemGroup.cs`
- `BovineLabs.Core.Extensions/Groups/AfterSceneSystemGroup.cs`
- `BovineLabs.Core.Extensions/Groups/BeginSimulationSystemGroup.cs`
- `BovineLabs.Core.Extensions/Groups/BeforeTransformSystemGroup.cs`
- `BovineLabs.Core.Extensions/Groups/AfterTransformSystemGroup.cs`
- `BovineLabs.Core.Extensions/Groups/RelevancySystemGroup.cs`
- `BovineLabs.Core.Extensions/Groups/BLSimulationSystemGroup.cs`
- `Documentation~/LifeCycle.md`
- `Documentation~/Pause.md`

## Core Rule

Choose the group from data timing, world phase, and pause policy first.

Do not start with `UpdateAfter` or `UpdateBefore` and hope ordering attributes will compensate for the wrong group.

## Group Selection

### `BeforeSceneSystemGroup`

Use this when the system must run before `SceneSystemGroup`.

Good fit:

- work that must affect entities before scene unload can remove them
- initialization that does not depend on freshly loaded subscene entities
- lifecycle or post-load plumbing that must happen ahead of scene processing

Bad fit:

- systems that need newly loaded subscene entities

### `AfterSceneSystemGroup`

Use this when the system depends on entities that were just loaded from subscenes.

Good fit:

- registry/bootstrap systems for freshly loaded content
- subscene load orchestration
- settings/bootstrap flows that depend on scene-loaded entities

### `BeginSimulationSystemGroup`

Use this sparingly for setup data needed broadly at the start of simulation.

Good fit:

- input-like or early frame setup used by many later systems
- start-of-simulation data refresh

Bad fit:

- ordinary gameplay systems that just happen to want to run "early"

### `BeforeTransformSystemGroup`

Use this for simulation systems that write `LocalTransform`.

If the system mutates transform data that should feed the transform update, it belongs here.

### `AfterTransformSystemGroup`

Use this for simulation systems that require up-to-date transform results and do not write `LocalTransform`.

This is the normal home for transform readers and post-transform consumers.

### `RelevancySystemGroup`

Use this only for server-side NetCode ghost relevancy work.

It runs under `AfterTransformSystemGroup`, server worlds only.

Do not use it as a generic "network" or "post-transform" bucket.

### `BLSimulationSystemGroup`

Use this as the base for custom root simulation groups that should disable during normal pause.

It already implements `IDisableWhilePaused`.

## Ordering Rules

- Pick the correct group first.
- Then use `UpdateAfter` and `UpdateBefore` to order peers inside that phase.
- Keep ordering local and explainable.
- If ordering spans multiple phases, the group choice is probably wrong.

## Lifecycle Boundary

The lifecycle skill owns detailed initialize/destroy sequencing.

Use this system-groups skill when the question is "where should this system live?"
Use the lifecycle skill when the question is "how should initialize/destroy phases behave?"

They overlap, but they are not the same skill.

## Pause Boundary

Most simulation groups in `core` derive from `BLSimulationSystemGroup`, so placement affects normal pause behavior.

- If a whole custom simulation root should stop during normal pause, derive from `BLSimulationSystemGroup`.
- If a child system must still update while paused, mark the child with `IUpdateWhilePaused` rather than inventing a new root group.
- If the whole feature must stay active while paused, do not put it under a pause-disabled root group casually.

## World Filter Rules

Check the built-in `WorldSystemFilter` on the group before adding systems.

Examples:

- `BeforeSceneSystemGroup` and `AfterSceneSystemGroup` are simulation-world groups.
- `BeginSimulationSystemGroup` has special simulation/menu/editor scope.
- `RelevancySystemGroup` is server-only.

If a system needs a different world scope than the target group, re-evaluate the placement.

## Common Patterns

### Freshly Loaded Scene Content

If the system needs scene-loaded entities:

- start with `AfterSceneSystemGroup`
- order within that phase if needed

### Pre-Unload Or Pre-Scene Work

If the system must run before `SceneSystemGroup`:

- start with `BeforeSceneSystemGroup`

### Transform Writer

If it writes `LocalTransform`:

- start with `BeforeTransformSystemGroup`

### Transform Reader

If it depends on finished transform updates and does not write transforms:

- start with `AfterTransformSystemGroup`

### Custom Simulation Root

If you are introducing a package-level root simulation group:

- default to `BLSimulationSystemGroup` when normal pause should disable it
- otherwise justify a plain `ComponentSystemGroup`

## Failure Checklist

- System uses loaded scene content but is in a pre-scene group:
  move it toward `AfterSceneSystemGroup`.
- System writes transforms but runs after transform systems:
  move it toward `BeforeTransformSystemGroup`.
- System only needed a local peer order but was moved across phases:
  revert the phase move and use ordering attributes instead.
- Feature unexpectedly stops during pause:
  inspect whether its root group derives from `BLSimulationSystemGroup`.
- Server-only relevancy logic placed in a general simulation group:
  move it to `RelevancySystemGroup`.
