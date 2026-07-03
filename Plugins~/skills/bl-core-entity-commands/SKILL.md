---
name: bl-core-entity-commands
description: "Use for BovineLabs Core IEntityCommands wrappers across bakers, command buffers, parallel jobs, tests, shared components, or entity mutations."
---

# Core Entity Commands

Resolve Core source from `Packages/com.bovinelabs.core` or its exact package-cache entry. Use `IEntityCommands` to share pure entity-shape logic across baking, runtime, jobs, and tests.

```csharp
static void Build<T>(ref T commands, ...)
    where T : struct, IEntityCommands
```

## Choose The Wrapper

| Context | Wrapper | Important constraint |
|---|---|---|
| Baker/authoring | `BakerCommands` | No `Instantiate`, `SetName`, or `AppendToBuffer` |
| Main-thread deferred runtime | `CommandBufferCommands` | Changes apply at ECB playback |
| Parallel job | `CommandBufferParallelCommands` | Pass the execution context's stable sort key |
| Tests/editor/immediate mutation | `EntityManagerCommands` | Mutates the world immediately |

Read `Documentation~/EntityCommands.md` and the matching wrapper source when exact support differs.

## Builder Rules

- Keep reusable builders to adding/setting components, buffers, enableable state, unmanaged shared components, and blob registration.
- Perform queries, spawn selection, system ordering, and bake/runtime-specific orchestration outside the builder.
- `IEntityCommands.Entity` is mutable local state: `CreateEntity()` and `Instantiate(...)` replace it. Use explicit-entity overloads whenever a helper touches more than one entity.
- Use `AddBuffer` when a buffer may be absent and `SetBuffer` when replacing/clearing it. For baker-compatible builders, fill the returned buffer instead of calling `AppendToBuffer`.
- Shared component methods require an explicit entity. Use `AddSharedComponent` for first addition and `SetSharedComponent` for replacement.
- `AddBlobAsset` deduplicates through the baker automatically; other wrappers need a supplied `BlobAssetStore` when dedupe matters.

## Common Patterns

- One generic builder called from a baker, runtime ECB, and test setup.
- `EntityManagerCommands` in tests to exercise the same entity-shape logic used by production code.
- `CommandBufferParallelCommands` in jobs rather than downgrading reusable logic to the main thread.

## Failure Triage

- Baker throws: look for `Instantiate`, `SetName`, or `AppendToBuffer`.
- Components land on the wrong entity: check whether the local `Entity` changed.
- Blob dedupe fails outside baking: supply a `BlobAssetStore`.
- Parallel code cannot reuse the helper: constrain it to `T : struct, IEntityCommands` and pass the sort key.
- A helper needs queries or unrelated policy: split orchestration from entity construction.
