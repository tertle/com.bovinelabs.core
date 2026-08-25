# Extensions

Most Core extensions are in:

```csharp
using BovineLabs.Core.Extensions;
```

This page is a curated map of the public APIs. Prefer Unity's built-in API when it already expresses the operation; use the unsafe Core helpers only when their additional behavior is required.

## Entity, query, and system APIs

| Need | API | Important members |
|---|---|---|
| Immediate entity-manager access | `EntityManagerExtensions` | `GetComponentLookup<T>`, `GetBufferLookup<T>`, singleton access, `GetUntypedBuffer`, named `CreateEntity` overloads |
| Low-level system state | `SystemStateExtensions` | `GetSharedComponentLookup<T>`, `GetChangeFilterLookup<T>`, `GetUnsafeEnableableLookup`, `GetUnsafeEntityDataAccess`, `AddDependency` |
| Shared-filter inspection | `EntityQueryExtensions` | `QueryHasSharedFilter<T>`, `ReplaceSharedComponentFilter<T>` |
| Runtime-type query construction | `EntityQueryBuilderExtensions` | `WithAll`, `WithAllRW`, `WithAny`, `WithAnyRW`, `WithNone` using `ComponentType` |
| Chunk enable masks and dynamic buffer types | `ArchetypeChunkExtensions` | `GetDynamicBufferAccessor`, enabled-bit access, `GetEnabledMaskRO`, `ChunkIndex` |
| Untyped ECB operations | `EntityCommandBufferExtensions` | `AddUntypedBuffer`, `UnsafeAddComponent` |

### EntityManager singleton access

`EntityManagerExtensions` includes:

- `HasSingleton<T>()`
- `GetSingleton<T>(completeDependency: true)`
- `GetSingletonRW<T>(completeDependency: true)`
- `TryGetSingleton<T>(out value, completeDependency: true)`
- `GetSingletonBuffer<T>(isReadOnly)`
- `GetSingletonBufferNoSync<T>(isReadOnly)`

The default accessors complete relevant dependencies. The `NoSync` form does not; the caller must already own the correct dependency ordering.

Immediate `EntityManager` mutation is not job-safe and can cause structural changes. Use an entity command buffer for deferred runtime structural work. See [IEntityCommands](EntityCommands.md) for reusable entity-shape builders.

### Lookups

Core no longer defines `ComponentLookupExtensions` or `BufferLookupExtensions`. Use Unity's lookup APIs directly:

```csharp
var optional = componentLookup.GetRefRWOptional(entity);
if (optional.IsValid)
{
    optional.ValueRW.Value++;
}

if (bufferLookup.TryGetBuffer(entity, out var buffer))
{
    // Use buffer.
}
```

The old `GetOptionalComponentDataRW<T>` and `GetRefRWNoChangeFilter<T>` helpers are not current Core APIs. Unity's `GetRefRWOptional` is the supported optional-ref replacement; acquiring write access follows Unity's normal change-version rules.

Public Core lookup utilities and their refresh rules are covered in [Iterators](Iterators.md).

## Native arrays, lists, and buffers

| Type | Extension class | Common operations |
|---|---|---|
| `NativeArray<T>` | `NativeArrayExtensions` | ref access, `ElementAtRO`, `Fill`, `Clear`, `Reverse`, `Clone`, predicate-based `Where` / `Select` / `Any` / `All` |
| `NativeList<T>` | `NativeListExtensions` | `ReserveNoResize`, `Insert`, `ResizeInitialized`, managed-array/enumerable `AddRange` |
| `DynamicBuffer<T>` | `DynamicBufferExtensions` | `ResizeInitialized`, pointer `AddRange`, `InsertAllocate`, `GetPtr`, explicit access checks |
| `UnsafeList<T>` | `UnsafeListExtensions` | `ReserveNoResize`, predicate `IndexOf` and `TryGetValue` |
| `List<T>` | `ListExtensions` | `AddRangeNative`, `ClearAddRange`, `Resize` |

### Ref and pointer lifetimes

```csharp
ref var value = ref values.ElementAt(index);
ref readonly var readOnlyValue = ref values.ElementAtRO(index);
var pointer = values.ElementAtAsPtr(index);
```

Refs and pointers are aliases into the container. They become invalid when the container is disposed or reallocated and must not outlive the job or scope that owns the container.

`DynamicBufferExtensions.AddRange` accepts a pointer and length. `InsertAllocate` returns a pointer to newly inserted storage. Both require an unsafe context and writable buffer access.

There is no current `AsNativeArrayRO<T>` Core extension. Use Unity's `DynamicBuffer<T>.AsNativeArray()` and preserve the read-only access mode supplied by the query or lookup.

## Hash maps and sets

| Container | Extension class | Selected operations |
|---|---|---|
| `NativeHashMap<TKey, TValue>` | `NativeHashMapExtensions` | `GetOrAddRefUnsafe`, `Remove(key, out value)` |
| `NativeHashSet<TKey>` | `NativeHashSetExtensions` | bucket reset/recalculation and low-level key access |
| `NativeParallelHashMap<TKey, TValue>` | `NativeParallelHashMapExtensions` | reserve, batch population, bucket recalculation, ref access |
| `NativeParallelMultiHashMap<TKey, TValue>` | `NativeParallelMultiHashMapExtensions` | unique-key collection, reserve, batch population, bucket recalculation |
| `NativeParallelHashSet<TKey>` | `NativeParallelHashSetExtensions` | reserve, copy to list, batch population, first-key lookup |
| `UnsafeHashMap<TKey, TValue>` | `UnsafeHashMapExtensions` | ref get-or-add and remove-with-value |
| `UnsafeParallelHashMap<TKey, TValue>` | `UnsafeParallelHashMapExtensions` | per-thread writer, ref access, batch population |

There is no separate `UnsafeParallelMultiHashMapExtensions` class.

Methods named `Unsafe` frequently write directly into key/value storage, bypass duplicate checks, or require pre-reserved capacity. Read the method's source contract before using one. In particular, a ref returned by `GetOrAddRefUnsafe` or `GetRef` is invalid after any later map write that can relocate or reorganize storage.

For entity-backed maps, use [DynamicHashMap](DynamicHashMap.md). For direct parallel traversal, use [Jobs](Jobs.md).

## Queues and streams

- `NativeQueueExtensions.IsCreated` checks a `NativeQueue<T>.ParallelWriter`.
- `NativeStreamExtensions.WriteLarge` and `ReadLarge` split data that exceeds a single native-stream allocation block.
- `BufferAccessorExtensions.GetUnsafe` exposes a dynamic buffer from a `BufferAccessor<T>` without the normal accessor path.

The large stream and unsafe buffer APIs operate on raw memory. Keep element layout, reader/writer position, and source lifetime identical on both sides.

## Worlds and systems

`WorldExtensions` supports both `World` and `WorldUnmanaged`:

- `IsThinClientWorld()`
- `IsClientWorld()`
- `IsServerWorld()`
- `IsServerLocalWorld()`
- `IsEditorWorld()`

`WorldUnmanagedExtensions` adds `SystemExists<T>()` and the advanced `GetTrackedJobHandle()` diagnostic helper. Core does not currently define `ComponentSystemBaseExtensions`.

## Math, bounds, physics, and scenes

| API | Operations |
|---|---|
| `MathematicsExtensions` | `AABB.Encapsulate`, `Expand`, `IsDefault`, matrix/quaternion direction vectors, matrix position and rotation |
| `AabbExtensions` | In-place `Shrink`, `ShrinkSafe`, `ExpandX`, `ExpandY`, `ExpandZ` for `Aabb` |
| `MinMaxAABBExtensions` | `Overlaps` |
| `PhysicsExtensions` | `Plane.Raycast` and `Ray.GetPoint` |
| `EntitySceneReferenceExtensions` | `SceneGUID` |

`RayExtension` is not a current type; the ray helpers live in `PhysicsExtensions`.

## Strings, objects, and enumerables

- `StringExtensions`: sentence/dot notation, casing, prefix/suffix trimming, bounded length, and no-error fixed-string conversion.
- `GameObjectExtensions`: `IsPrefab` and `IsAsset` authoring checks.
- `EnumerableExtensions`: value- and predicate-based `IndexOf`.

## Clearing jobs and factories

The `ContainerClearJobs.cs` file defines concrete jobs such as `ClearListJob<T>`, `ClearNativeHashMapJob<TKey, TValue>`, `ClearNativeParallelHashMapJob<TKey, TValue>`, and `ClearNativeParallelMultiHashMapJob<TKey, TValue>`.

For non-default hash-map growth, use the generic factories:

```csharp
var map = NativeHashMapFactory<int, Entity>.Create(
    initialCapacity: 128,
    minGrowth: 64,
    allocator: allocator);

var set = NativeHashSetFactory<Entity>.Create(
    initialCapacity: 128,
    minGrowth: 64,
    allocator: allocator);
```

The caller owns and must dispose the returned container.

## Unsafe checklist

- Keep every pointer/ref alias within the source container's lifetime and before its next possible reallocation.
- Register component dependencies before using `UnsafeEntityDataAccess` or dynamic `ComponentType` access in jobs.
- Pre-size destinations before no-resize or direct bucket writes.
- Do not assume batch hash-map helpers validate duplicates or preserve order.
- Chain every scheduled clear, fill, or traversal handle before reusing or disposing the container.
