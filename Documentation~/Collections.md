# Collections

Core extends Unity Collections with fixed-storage values, keyed maps, fallback writers, thread-local containers, blob collections, entity-owned maps, and low-level unsafe views.

Most collection bugs are lifetime or concurrency bugs. Choose the smallest ownership model that fits the data before choosing by API shape.

```csharp
using BovineLabs.Core.Collections;
```

## Choose by ownership

| Data lifetime | Prefer | Owner |
|---|---|---|
| A field-sized value with no allocation | `FixedArray`, `FixedBitMask`, `FixedHashMap`, or `BitArray*` | Containing value/component |
| A normal allocator-backed native container | A `Native*` Core or Unity collection | Creating system or scope |
| Read-only data shared through a blob asset | `BlobHashMap`, `BlobMultiHashMap`, `BlobPerfectHashMap`, curves, or splines | Blob asset reference/store |
| A dictionary or set stored on one entity | [Entry-backed dynamic collections](DynamicCollections.md) | Entity's `DynamicBuffer<T>` |
| A specialized byte-backed map on one entity | [Generated dynamic hash maps](DynamicHashMap.md) | Entity's `DynamicBuffer<byte>` |
| A short-lived list reused on the current thread | [`PooledNativeList<T>`](PooledNativeList.md) | `using` scope, then thread-local pool |
| Many producers and one frame consumer | [`SingletonCollectionUtil`](SingletonCollection.md) | Owning system and rewindable allocator |
| Maximum control with safety disabled | `Unsafe*` collection or lookup | Expert caller |

## Fixed-storage values

| Type | Use it for |
|---|---|
| `FixedArray<T, TStorage>` | Small inline arrays whose byte capacity is supplied by an unmanaged storage type |
| `FixedBitMask<TStorage>` | A fixed number of bit flags stored in a caller-selected unmanaged value |
| `FixedHashMap<TKey, TValue, TCapacity>` | A small inline hash map with compile-time storage |
| `BitArray8`, `BitArray16`, `BitArray32`, `BitArray64`, `BitArray128`, `BitArray256` | Serializable fixed-capacity bit arrays with indexing and bitwise operations |
| `MiniString` | Compact UTF-8 text whose storage must fit in a small unmanaged value |

These values do not use an allocator. Capacity is fixed; check the type's behavior when full rather than assuming it can grow.

## Keyed and perfect maps

`NativeKeyedMap<TValue>` and `UnsafeKeyedMap<TValue>` optimize integer-key grouping when the maximum key is known. They allocate storage proportional to the key range, so they are a poor fit for sparse, very large IDs.

`NativePartialKeyedMap<TValue>` and `UnsafePartialKeyedMap<TValue>` build keyed lookup state over caller-supplied key/value memory. Use them only when the pointed-to arrays and the map share a proven lifetime.

`NativePerfectHashMap<TKey, TValue>` and `UnsafePerfectHashMap<TKey, TValue>` target fixed key sets. Build them when lookup speed matters more than mutation; do not treat them as general growable dictionaries.

## Hash maps and overflow fallback

Core includes:

- `NativeMultiHashMap<TKey, TValue>` and `UnsafeMultiHashMap<TKey, TValue>`.
- `NativeParallelHashMapFallback<TKey, TValue>`.
- `NativeParallelMultiHashMapFallback<TKey, TValue>`.
- `NativeUntypedHashMap` for data selected by runtime type.

The fallback maps pair a fixed-capacity parallel map with a queue. Parallel writers enqueue entries that cannot reserve a slot; `Apply(...)` folds the fallback queue into a resized map before readers use it. Chain the writer dependency into `Apply` and use the returned read-only view only after that handle.

## Thread-local and work-processing containers

| Type | Behavior |
|---|---|
| `ThreadList` | One unsafe list per Unity worker-thread index |
| `ThreadRandom` | One cache-line-aligned `Unity.Mathematics.Random` per thread; non-deterministic across schedules |
| `NativeThreadStream` / `UnsafeThreadStream` | Parallel per-thread streams that can write mixed unmanaged values or typed sequences |
| `NativeWorkQueue<T>` | Fixed-capacity concurrent work queue with explicit update/reset behavior |
| `NativeLinearCongruentialGenerator` | Fast parallel-friendly generated values for workloads that do not require deterministic `Random` streams |
| `NativeCounter` | Allocator-backed atomic counter |

Unity thread-index containers are valid only on the main thread or Unity worker threads. Do not use them from arbitrary managed threads.

For a globally initialized convenience wrapper over `ThreadRandom`, see [Global random](GlobalRandom.md).

## Entity buffer views

Core supplies typed and untyped access for advanced ECS code:

- `UnsafeDynamicBuffer<T>`.
- `UntypedDynamicBuffer` and `UnsafeUntypedDynamicBuffer`.
- `DynamicBufferAccessor` and `UnsafeUntypedDynamicBufferAccessor`.
- `UnsafeHashMapBucketData<TKey, TValue>` for low-level bucket traversal.

These APIs expose memory owned by an ECS chunk or buffer. They do not extend that storage's lifetime. A structural change, buffer resize, or invalid dependency can invalidate a cached pointer or view.

For normal entity-owned dictionaries and sets, prefer [dynamic buffer collections](DynamicCollections.md).

## Blob collections

Core adds immutable lookup structures built with `BlobBuilder`:

- `BlobHashMap<TKey, TValue>`.
- `BlobMultiHashMap<TKey, TValue>`.
- `BlobPerfectHashMap<TKey, TValue>`.
- `BlobCurve`, `BlobCurve2`, `BlobCurve3`, and `BlobCurve4` plus their samplers.
- `BlobSpline` when the Splines integration is available.

Build blob storage during baking or managed setup, register/deduplicate it through the owning baker or `BlobAssetStore`, and retain the resulting `BlobAssetReference<T>` for as long as readers need it.

## Pooling and arena references

`UnmanagedPool<T>` manages reusable unmanaged values from an allocator-backed pool.

`Reference<T>` is a pointer-backed value similar to a blob reference. It is not a managed object wrapper. `Reference<T>.Create(...)` allocates through Core's `MemoryAllocator`; that allocator owns and eventually frees the referenced memory.

`PooledNativeList<T>` reuses `NativeList` allocations in a per-thread pool. See its [lifetime rules](PooledNativeList.md) before using it in jobs.

## Entity-owned lookup collections

Core has two separate dynamic-buffer families:

| Family | Storage | Best fit |
|---|---|---|
| `DynamicDictionary`, `DynamicMultiDictionary`, `DynamicHashSet` | Typed entry buffer | Normal component data, explicit entry layout, dictionary/set semantics |
| `DynamicHashMap`, `DynamicMultiHashMap`, `DynamicHashSet` in `BovineLabs.Core.Iterators` | Generated byte buffer | Specialized variants, compact raw layout, optional generated NetCode transport |

The similarly named hash-set wrappers live in different namespaces and use different marker interfaces. Read [Dynamic buffer collections](DynamicCollections.md) and [Generated dynamic hash maps](DynamicHashMap.md) before selecting one.

## Common extension operations

`BovineLabs.Core.Extensions` adds operations for Unity and Core collections, including resizing initialized buffers, reserving ranges, copying keys, batch insertion, and unsafe ref access.

Methods named `GetOrAddRefUnsafe` return a reference into container storage. Consume it immediately. Any later write to the same container can resize or rehash and invalidate the reference. See [Extensions](Extensions.md).

## Lifetime checklist

1. Record who owns the allocation before scheduling work.
2. Pass every outstanding reader/writer as a dependency to the next operation.
3. Dispose allocator-owned native/unsafe collections after the final dependency.
4. Do not dispose ECS buffer wrappers or blob values whose owner manages the storage.
5. Do not retain raw pointers, `NativeArray` views, iterators, or returned refs across a resize, rehash, structural change, or allocator rewind.
6. Prefer checked native wrappers until profiling proves the unsafe variant is needed.

## Related guides

- [Dynamic buffer collections](DynamicCollections.md)
- [Generated dynamic hash maps](DynamicHashMap.md)
- [Singleton collections](SingletonCollection.md)
- [PooledNativeList](PooledNativeList.md)
- [Iterators](Iterators.md)
- [Jobs](Jobs.md)
