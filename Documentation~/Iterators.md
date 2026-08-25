# Iterators

Core provides focused iterators for dynamic-buffer maps, blob maps, entity queries, and enabled-mask-aware chunk work.

## Choose an iterator

| Data or workload | API |
|---|---|
| Every entry in a dynamic hash map or multimap | `foreach` / `DynamicHashMapEnumerator<TKey, TValue>` |
| Values for one multimap key | `GetValuesForKey(key)` / `DynamicHashMapKeyEnumerator<TKey, TValue>` |
| Every entry in a dynamic hash set | `DynamicHashSetEnumerator<T>` |
| Untyped unsafe dynamic map | `GetUntypedIterator(...)` / `UntypedDynamicHashMapIterator` |
| Every entry in a blob hash map or multimap | `BlobHashMapEnumerator<TKey, TValue>` |
| Values for one blob multimap key | `TryGetFirstValue` / `TryGetNextValue` with `BlobMultiHashMapIterator<TKey>` |
| Main-thread traversal of an `EntityQuery` | `QueryEntityEnumerator` |
| Reusable enabled-mask handling inside a chunk job | `CustomChunkIterator<T>` |

For parallel hash-map traversal without copying keys, see [Jobs](Jobs.md).

## Dynamic hash maps

Dynamic-map wrappers are in `BovineLabs.Core.Iterators`. Obtain them from the generated `AsMap()` extension described in [DynamicHashMap](DynamicHashMap.md).

```csharp
var inventory = buffer.AsMap();

foreach (var pair in inventory)
{
    var key = pair.Key;
    var value = pair.Value;
}
```

For a multimap, the per-key method is `GetValuesForKey`:

```csharp
foreach (var value in damageBySource.GetValuesForKey(sourceId))
{
    totalDamage += value;
}
```

The main iterator types are:

| Type | Purpose |
|---|---|
| `DynamicHashMapEnumerator<TKey, TValue>` | All map or multimap key-value pairs |
| `DynamicHashMapKeyEnumerator<TKey, TValue>` | All multimap values for one key |
| `DynamicHashSetEnumerator<T>` | All set values |
| `UntypedDynamicHashMapIterator` | Unsafe pointer pairs from an untyped dynamic map |

The `KVPair<TKey, TValue>.Value` returned by the typed dynamic-map enumerator is a ref into the backing buffer. Do not retain it after a resize or other operation that can relocate the buffer.

## Blob hash maps

`BlobHashMap<TKey, TValue>` and `BlobMultiHashMap<TKey, TValue>` are in `BovineLabs.Core.Collections`.

```csharp
ref var map = ref blobReference.Value.Map;

foreach (var pair in map)
{
    var key = pair.Key;
    var value = pair.Value;
}
```

For a single multimap key:

```csharp
if (map.TryGetFirstValue(key, out var value, out var iterator))
{
    do
    {
        ref readonly var item = ref value.Ref;
        // Read item.
    }
    while (map.TryGetNextValue(out value, ref iterator));
}
```

`BlobMultiHashMapIterator<TKey>` is traversal state for the `TryGet*` methods; it is not itself a `foreach` enumerator. Blob data is immutable after construction, even though low-level accessors may expose refs.

## Entity query enumeration

`QueryEntityEnumerator` is a main-thread, allocation-free chunk iterator in `BovineLabs.Core.Utility`. It produces Unity's `ChunkEntityEnumerator` so enableable-component masks are handled correctly.

```csharp
var queryEnumerator = new QueryEntityEnumerator(query);

while (queryEnumerator.MoveNextChunk(out var chunk, out var entityEnumerator))
{
    while (entityEnumerator.NextEntityIndex(out var entityIndex))
    {
        // Read chunk arrays at entityIndex.
    }
}
```

Call `Reset()` to traverse the same query again. Do not perform structural changes that invalidate query chunk storage during traversal.

## Custom chunk iteration

`CustomChunkIterator<T>` does not accept a query or a delegate. `T` must be an unmanaged `ICustomChunkIterator` implementation, supplied to the constructor. The wrapper only centralizes dense and enabled-mask iteration for one already-resolved chunk.

```csharp
public struct Counter : IComponentData, IEnableableComponent
{
    public int Value;
}

[BurstCompile]
private struct ProcessChunkJob : IJobChunk
{
    public ComponentTypeHandle<Counter> CounterHandle;

    public void Execute(
        in ArchetypeChunk chunk,
        int unfilteredChunkIndex,
        bool useEnabledMask,
        in v128 chunkEnabledMask)
    {
        var executor = new Increment
        {
            Counters = chunk.GetNativeArray(ref this.CounterHandle),
        };

        new CustomChunkIterator<Increment>(executor)
            .Execute(chunk, useEnabledMask, chunkEnabledMask);
    }

    private struct Increment : ICustomChunkIterator
    {
        public NativeArray<Counter> Counters;

        public void Execute(int entityIndexInChunk)
        {
            var counter = this.Counters[entityIndexInChunk];
            counter.Value++;
            this.Counters[entityIndexInChunk] = counter;
        }
    }
}
```

The executor is copied into the wrapper. Put observable results in native containers or chunk arrays rather than expecting mutations to the executor struct itself to be returned.

## Lookup utilities

The public `SystemState` entry points are in `BovineLabs.Core.Extensions`.

| Lookup | Create | Refresh when cached | Use |
|---|---|---|---|
| `SharedComponentLookup<T>` | `state.GetSharedComponentLookup<T>(isReadOnly)` | `lookup.Update(ref state)` | Read unmanaged shared components by entity or shared index |
| `ChangeFilterLookup<T>` | `state.GetChangeFilterLookup<T>(isReadOnly)` | `lookup.Update(ref state)` | Test or set chunk change versions through an entity |
| `UnsafeEnableableLookup` | `state.GetUnsafeEnableableLookup()` | No update method | Dynamic `ComponentType` enable/disable access |
| `UnsafeEntityDataAccess` | `state.GetUnsafeEntityDataAccess()` | `access.Update(ref state)` for current write version | Low-level typed-by-`ComponentType` pointers and untyped buffers |

Example of a cached shared-component lookup:

```csharp
private SharedComponentLookup<Team> teamLookup;

public void OnCreate(ref SystemState state)
{
    this.teamLookup = state.GetSharedComponentLookup<Team>(isReadOnly: true);
}

public void OnUpdate(ref SystemState state)
{
    this.teamLookup.Update(ref state);
}
```

`UnsafeEnableableLookup` and `UnsafeEntityDataAccess` bypass parts of Unity's typed safety tracking. Register every accessed component with `state.AddDependency(...)`, chain job handles, and ensure parallel writes cannot target the same data.

`UnsafeComponentLookup<T>` and `UnsafeBufferLookup<T>` exist as Core implementation types, but their construction helpers are currently internal to the Core assembly. They are not general consumer entry points; use Unity's `ComponentLookup<T>` / `BufferLookup<T>` or the public utilities above.

## Lifetime and mutation rules

- Do not resize, clear, or remove from a dynamic map while one of its enumerators or ref values is in use.
- Do not keep an iterator after its dynamic buffer, blob asset, query, or container has become invalid.
- A copied dynamic-map wrapper can become stale when another copy resizes the backing buffer. Keep mutation on one live wrapper.
- Respect enableable masks. Use `ChunkEntityEnumerator` or `CustomChunkIterator<T>` instead of a raw `0..chunk.Count` loop when `useEnabledMask` is true.
- Unsafe untyped iterators expose pointers, not ownership. Their source storage must outlive every access.

See [DynamicHashMap](DynamicHashMap.md) for initialization and mutation APIs and [Collections](Collections.md) for the underlying collection types.
