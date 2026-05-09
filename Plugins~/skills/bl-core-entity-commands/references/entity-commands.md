# Core Entity Commands Reference

## Read First

Use these sources when the task needs exact behavior:

- `Documentation~/EntityCommands.md`
- `BovineLabs.Core/EntityCommands/IEntityCommands.cs`
- `BovineLabs.Core/EntityCommands/EntityManagerCommands.cs`
- `BovineLabs.Core/EntityCommands/CommandBufferCommands.cs`
- `BovineLabs.Core/EntityCommands/CommandBufferParallelCommands.cs`
- `BovineLabs.Core.Authoring/EntityCommands/BakerCommands.cs`

## Core Pattern

The main value of `IEntityCommands` is shared builder logic across contexts.

Prefer helpers like:

```cs
static void Build<T>(ref T commands, ...)
    where T : struct, IEntityCommands
```

Use this when the entity setup should work in more than one context:

- baking
- runtime main thread with `EntityCommandBuffer`
- runtime jobs with `EntityCommandBuffer.ParallelWriter`
- tests or editor-time immediate setup

Do not tie a builder to `IBaker`, `EntityManager`, or `EntityCommandBuffer` unless the logic genuinely needs APIs outside `IEntityCommands`.

## Choose The Concrete Implementation

### `BakerCommands`

Use in bakers and authoring conversions.

Good fit:

- adding components and buffers during baking
- shared builder code reused between bakers and runtime/tests
- blob registration through the baker

Important limitations:

- `Instantiate` is not supported
- `SetName` is not supported
- `AppendToBuffer` is not supported

For buffers in a baker, use `AddBuffer` or `SetBuffer` and fill the returned buffer directly.

### `CommandBufferCommands`

Use for main-thread deferred runtime work with an `EntityCommandBuffer`.

Good fit:

- systems building or instantiating entities on the main thread
- shared setup logic before ECB playback

### `CommandBufferParallelCommands`

Use inside jobs with `EntityCommandBuffer.ParallelWriter`.

Good fit:

- parallel jobs that need shared builder logic
- chunk or entity iteration where a stable sort key is available

Always pass the correct sort key from the execution context.

### `EntityManagerCommands`

Use for immediate execution contexts.

Good fit:

- tests
- editor-only setup
- immediate world mutation where deferred playback is not wanted

## Local Entity Rules

`IEntityCommands.Entity` is the default target for overloads that do not take an explicit entity.

Important consequences:

- `CreateEntity()` replaces the stored local entity.
- `Instantiate(prefab)` replaces the stored local entity.
- `AddComponent(component)` and similar overloads act on the current stored entity.

If a helper touches more than one entity, use the explicit `Entity` overloads instead of relying on the mutable local target implicitly.

## Shared Builder Guidance

Prefer builders that only express entity shape and setup.

Good builder responsibilities:

- add components
- set component values
- add or fill buffers
- register blob assets
- toggle enableable components
- add or set unmanaged shared components

Poor builder responsibilities:

- world queries
- system ordering
- unrelated spawn-selection policy
- bake-only or runtime-only control flow mixed into the reusable helper

Choose the entity externally, then let the builder populate it.

## Buffer Rules

- Use `AddBuffer` when the buffer may not exist yet.
- Use `SetBuffer` when the buffer should be replaced or cleared before refill.
- Use `AppendToBuffer` only in contexts where the concrete implementation supports it.
  `BakerCommands` does not.

If you need a builder to work in bakers and runtime, prefer `AddBuffer` or `SetBuffer` plus direct writes to the returned buffer.

## Shared Component Rules

`IEntityCommands` supports unmanaged shared components across all concrete wrappers:

- `AddSharedComponent<T>(Entity entity, in T component)`
- `SetSharedComponent<T>(Entity entity, in T component)`

Use these instead of dropping to `IBaker`, `EntityCommandBuffer`, or `EntityManager` only because a builder needs shared component data.

Shared component operations only expose explicit-entity overloads. Pass the entity intentionally instead of relying on the mutable local `Entity` target.

Use `AddSharedComponent` when the entity should not already have that shared component. Use `SetSharedComponent` when replacing the shared component value on an entity that already has it.

## Blob Asset Rules

`AddBlobAsset` is the cross-context hook for blob registration and deduping.

- `BakerCommands` routes to `IBaker.AddBlobAsset`.
- `EntityManagerCommands`, `CommandBufferCommands`, and `CommandBufferParallelCommands` only dedupe when a `BlobAssetStore` was supplied.

If blob dedupe matters in tests or immediate runtime setup, pass a `BlobAssetStore` into the concrete command wrapper.

If the task is primarily about blob layout or builder correctness, also use the blobs skill.

## Common Patterns

### Shared Authoring And Runtime Builder

Write one generic builder over `T : struct, IEntityCommands`, then call it from:

- a baker with `BakerCommands`
- a runtime system with `CommandBufferCommands`
- tests with `EntityManagerCommands`

### Test Fixture Setup

Use `EntityManagerCommands` in tests when you want the same builder used by bakers or runtime logic.

Pass a `BlobAssetStore` if the builder registers blobs.

### Parallel Job Setup

Use `CommandBufferParallelCommands` and pass the job's sort key.

Do not downgrade to a main-thread builder just because a helper was written against a concrete non-parallel type.

## Failure Checklist

- Builder works in runtime but throws in a baker:
  check for `Instantiate`, `SetName`, or `AppendToBuffer`.
- Components end up on the wrong entity:
  check whether `CreateEntity()` or `Instantiate()` changed the local `Entity`.
- Shared component setup forced a concrete wrapper:
  use the explicit-entity `IEntityCommands` shared component methods instead.
- Blob dedupe does not happen outside baking:
  check whether the wrapper was created with a `BlobAssetStore`.
- Job version cannot reuse the builder:
  switch the helper to `T : struct, IEntityCommands` and use `CommandBufferParallelCommands`.
- Helper mixes world queries with pure entity setup:
  split selection/orchestration from the reusable builder.
