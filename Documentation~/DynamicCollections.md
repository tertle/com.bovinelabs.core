# Dynamic buffer collections

Core's entry-backed dynamic collections store a dictionary, multi-dictionary, or set directly in an ECS `DynamicBuffer<TEntry>`. The buffer element remains a normal, typed component while the wrapper supplies hash-table lookup behavior.

Use this family for most new entity-owned maps whose entries should remain explicit component data. Use [generated dynamic hash maps](DynamicHashMap.md) when a specialized byte-backed layout, variable-value map, perfect hash map, or generated NetCode serializer is required.

## Choose a collection

| Entry interface | Wrapper | Semantics |
|---|---|---|
| `IDynamicDictionaryEntry<TKey, TValue>` | `DynamicDictionary<TKey, TValue, TEntry>` | One value per key; indexer add/update and `TryAdd` |
| `IDynamicMultiDictionaryEntry<TKey, TValue>` | `DynamicMultiDictionary<TKey, TValue, TEntry>` | Multiple values and duplicate pairs per key |
| `IDynamicHashSetEntry<TKey>` | `DynamicHashSet<TKey, TEntry>` | Unique keys |

All three are Burst-compatible, grow through their backing buffer, use open addressing, and are not safe for concurrent writes.

## Define an entry

Entry types must be unmanaged, top-level buffer elements. The `Tag` field is collection-owned; application code should not interpret or modify it.

```csharp
namespace MyGame
{
    using BovineLabs.Core.Collections;
    using Unity.Entities;

    [InternalBufferCapacity(0)]
    public struct ItemById : IDynamicDictionaryEntry<int, ItemData>
    {
        public uint Tag { get; set; }

        public int Key { get; set; }

        public ItemData Value { get; set; }
    }

    public struct ItemData
    {
        public int Count;
        public float Weight;
    }
}
```

The source generator emits `AsMap()` for recognized entry types. The explicit `AsDynamicDictionary<TKey, TValue, TEntry>()` extension is also available.

## Create and populate the map

An empty buffer is a valid zero-capacity collection. Call `EnsureCapacity` when the expected size is known, or let the first insertion grow it.

```csharp
namespace MyGame.Authoring
{
    using BovineLabs.Core.Collections;
    using Unity.Entities;
    using UnityEngine;

    public sealed class ItemLookupAuthoring : MonoBehaviour
    {
        [Min(0)]
        public int InitialCapacity = 16;

        private sealed class Baker : Baker<ItemLookupAuthoring>
        {
            public override void Bake(ItemLookupAuthoring authoring)
            {
                var entity = this.GetEntity(TransformUsageFlags.None);
                var map = this.AddBuffer<ItemById>(entity).AsMap();

                map.EnsureCapacity(authoring.InitialCapacity);
                map.TryAdd(1, new ItemData { Count = 3, Weight = 1.5f });
            }
        }
    }
}
```

`EnsureCapacity` rounds up to a power of two and only grows. The collection may also grow or rehash automatically as entries and tombstones accumulate.

## Read and write in jobs

```csharp
namespace MyGame
{
    using BovineLabs.Core.Collections;
    using Unity.Burst;
    using Unity.Entities;

    [BurstCompile]
    public partial struct ConsumeItemJob : IJobEntity
    {
        private void Execute(DynamicBuffer<ItemById> items)
        {
            var map = items.AsMap();

            if (map.TryGetValue(1, out var item))
            {
                item.Count--;
                map[1] = item;
            }

            map.TryRemove(99);
        }
    }
}
```

The entity owns the buffer. Do not dispose the wrapper. Normal ECS dependency rules protect reads and writes to the buffer component.

## Dictionary operations

`DynamicDictionary` provides:

- `TryAdd`, `Add`, indexer get/set, `TryGetValue`, and `ContainsKey`.
- `TryRemove`, `Remove`, and `Clear`.
- `Count`, `Capacity`, `IsCreated`, and `IsEmpty`.
- `GetKeyArray`, `GetValueArray`, and `GetKeyValueArrays` for allocator-owned snapshots.
- `EnsureCapacity` and `ReconstructAfterRemap`.

`Add` throws for a duplicate key. `TryAdd` returns `false`. Setting the indexer adds a missing key or replaces the current value.

Dispose arrays returned by the snapshot methods or allocate them from an allocator whose lifetime is managed elsewhere.

## Multi-dictionary operations

```csharp
[InternalBufferCapacity(0)]
public struct DamageBySource : IDynamicMultiDictionaryEntry<int, float>
{
    public uint Tag { get; set; }

    public int Key { get; set; }

    public float Value { get; set; }
}
```

`Add` permits duplicate keys and duplicate key-value pairs. Iterate one key with the standard first/next pattern:

```csharp
var map = buffer.AsMap();

if (map.TryGetFirstValue(sourceId, out var damage, out var iterator))
{
    do
    {
        totalDamage += damage;
    }
    while (map.TryGetNextValue(out damage, ref iterator));
}
```

`Remove(key)` removes every value for that key and returns the removed count. `Remove(iterator)` removes the current pair. `CountValuesForKey` reports the number of matching pairs.

## Hash-set operations

```csharp
[InternalBufferCapacity(0)]
public struct UnlockedItem : IDynamicHashSetEntry<int>
{
    public uint Tag { get; set; }

    public int Key { get; set; }
}
```

Use `TryAdd` when duplicates are expected and `Add` when a duplicate should throw. `Contains`, `TryRemove`, `Remove`, `Clear`, and `GetKeyArray` provide the remaining common operations.

## Storage rules

- Buffer length is table capacity, not logical entry count.
- Raw slots include empty entries and tombstones. Use the wrapper rather than iterating the buffer directly.
- Capacity must be zero or a power of two. Prefer `EnsureCapacity` over manually resizing the buffer.
- The entry must be large enough for the collection's internal header. Normal dictionary entries satisfy this naturally; very small custom set entries may need padding.
- Collection writes can resize the buffer. Do not retain raw buffer pointers or views across later writes.
- These wrappers support one writer at a time. Schedule parallel work by partitioning ownership or gather writes into a separate concurrent container first.

## Remapped keys

An external remap can change key bytes without updating stored hash tags or slot positions. This commonly matters for `Entity` or other remapped identifiers during copying or serialization.

Call `ReconstructAfterRemap()` after the keys have changed and before the next lookup:

```csharp
var map = buffer.AsMap();
map.ReconstructAfterRemap();
```

## Troubleshooting

**`AsMap()` is missing**

Make the entry top-level, implement the exact entry interface, reference `BovineLabs.Core`, and fix earlier compiler errors. Use the explicit `AsDynamic*` extension while isolating generator issues.

**The map reports a non-power-of-two capacity**

Do not resize the raw buffer to an arbitrary length. Start empty and call `EnsureCapacity`.

**A key cannot be found after remapping**

Call `ReconstructAfterRemap()` once all key changes are complete.

**A read-only job throws a write-access error**

Only call query methods on a buffer acquired as read-only. Methods that add, remove, clear, resize, or reconstruct require write access.

## Related guides

- [Collections](Collections.md)
- [Generated dynamic hash maps](DynamicHashMap.md)
- [Iterators](Iterators.md)
