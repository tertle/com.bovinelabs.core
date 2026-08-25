# Generated dynamic hash maps

## Overview

The generated dynamic-hash-map family reinterprets a byte-oriented `DynamicBuffer<TBuffer>` as a hash map, multi-hash map, set, untyped map, variable-column map, or perfect hash map. Marker interfaces drive source generation for strongly typed `Initialize()` and `AsMap()` extensions.

Use this family when its specialized layout or optional generated NetCode serialization is required. For normal typed entries stored directly in a buffer, prefer [dynamic buffer collections](DynamicCollections.md).

```csharp
using BovineLabs.Core.Iterators;
```

## Choose between the two buffer-map families

| Need | Use |
|---|---|
| Typed dictionary/set entries that remain ordinary buffer elements | [`DynamicDictionary`, `DynamicMultiDictionary`, or entry-backed `DynamicHashSet`](DynamicCollections.md) |
| Compact byte-backed storage and generated accessors | This guide's `IDynamicHashMap`, `IDynamicMultiHashMap`, and related interfaces |
| Generated compact NetCode serialization | This guide's `IDynamicHashMap` or `IDynamicMultiHashMap` with `[GhostDynamicHashMap]` |
| Runtime-selected value types | `IDynamicUntypedHashMap<TKey>` |
| Extra value columns | `IDynamicVariableMap` |
| Fixed-size collision-free lookup | `IDynamicPerfectHashMap` |
| Heterogeneous sequential values in one buffer | `IDynamicUntypedBuffer` |

## Container Types

| Interface                                  | Purpose                       |
|--------------------------------------------|-------------------------------|
| `IDynamicHashMap<TKey, TValue>`            | Standard key-value dictionary |
| `IDynamicMultiHashMap<TKey, TValue>`       | Multiple values per key       |
| `IDynamicHashSet<TKey>`                    | Unique value set              |
| `IDynamicUntypedHashMap<TKey>`             | Variable value types          |
| `IDynamicPerfectHashMap<TKey, TValue>`     | Fixed-size collision-free map |
| `IDynamicVariableMap<TKey, TValue, T, TC>` | HashMap with extra column/s   |
| `IDynamicUntypedBuffer`                    | Heterogeneous sequential data |

## Basic Setup

Define a top-level marker buffer. The byte property exists only to satisfy `IBufferElementData`; application code uses the generated map wrapper.

```csharp
namespace MyGame
{
    using BovineLabs.Core.Iterators;
    using Unity.Entities;

    [InternalBufferCapacity(0)]
    public struct PlayerInventory : IDynamicHashMap<int, ItemData>
    {
        byte IDynamicHashMap<int, ItemData>.Value { get; }
    }

    public struct ItemData
    {
        public int Count;
    }
}
```

Initialize in your baker:

```csharp
var entity = baker.GetEntity(TransformUsageFlags.None);
var buffer = baker.AddBuffer<PlayerInventory>(entity);
buffer.Initialize();
```

Initialization establishes the byte layout. Call it once when adding the buffer during baking or controlled setup. Do not call `AsMap()` on an uninitialized buffer.

## Usage

Access the container in a job or system after initialization:

```csharp
[BurstCompile]
public partial struct UpdateInventoryJob : IJobEntity
{
    private void Execute(DynamicBuffer<PlayerInventory> buffer)
    {
        var inventory = buffer.AsMap();

        inventory[1] = new ItemData { Count = 3 };
        if (inventory.TryGetValue(1, out var item))
        {
            item.Count++;
            inventory[1] = item;
        }

        foreach (var kvp in inventory)
        {
            // Read kvp.Key and kvp.Value.
        }
    }
}
```

The entity owns the backing buffer. Do not dispose the wrapper. Writes are not thread-safe; schedule one writer through the buffer component dependency.

Keep a writable wrapper in a mutable local. Operations that resize the byte buffer refresh the wrapper's cached helper pointer; copied or readonly-held wrappers do not receive that update. Reacquire `AsMap()` after another wrapper or raw buffer operation may have resized the storage.

`AddBatchUnsafe` is a specialized fast path. It requires compatible key/value lengths and a layout state accepted by the helper. Prefer normal `Add`/indexer operations until profiling justifies it.

## Extension Methods

`AsMap` and `Initialize` extensions are generated for hash map, multi-hash map, hash set, untyped hash map, untyped buffer, and variable-map markers. Perfect hash maps receive generated `AsMap()` access, but initialization uses the explicit `InitializePerfectHashMap(...)` overload because it needs a source key set and default value.

If generation is missing, confirm the marker is top-level, implements the exact interface, and belongs to an assembly referencing `BovineLabs.Core`. Fix earlier compiler errors before diagnosing generator output.

## NetCode Dynamic Hash Map Serialization

When `com.unity.netcode` is installed, `IDynamicHashMap<TKey, TValue>` and `IDynamicMultiHashMap<TKey, TValue>` marker buffers can opt into compact
NetCode serialization with one attribute. The generator infers the collection kind from the implemented interface.
Generated serializers are registered as the default serializer for the marker buffer type, with display names derived from the marker type name.
The attribute does not add the buffer to a ghost or replicate it by itself; the marker buffer must be present on the ghost prefab.

```csharp
[GhostDynamicHashMap]
public struct PlayerInventory : IDynamicHashMap<int, byte>
{
    byte IDynamicHashMap<int, byte>.Value { get; }
}

[GhostDynamicHashMap]
public struct DamageBySource : IDynamicMultiHashMap<int, byte>
{
    byte IDynamicMultiHashMap<int, byte>.Value { get; }
}
```

Generated field encoding is the default. It writes deterministic key/value fields and omits padding from custom structs. A generated custom struct must be composed recursively from supported values and expose every instance field as public and writable. Generated mode rejects implicit backing fields, readonly or non-public fields, fixed buffers, explicit layout, pointers, `Entity`, `BlobAssetReference<T>`, and `Native*` container fields.

Use `RawStable` only when both key and value have stable raw byte representations:

```csharp
[GhostDynamicHashMap(CodecMode = GhostDynamicHashMapCodecMode.RawStable)]
public struct RawInventory : IDynamicHashMap<int, byte>
{
    byte IDynamicHashMap<int, byte>.Value { get; }
}
```

NetCode metadata uses the same public names as `GhostComponentAttribute`:

```csharp
[GhostDynamicHashMap(
    OwnerSendType = SendToOwnerType.SendToOwner,
    SendDataForChildEntity = true)]
public struct OwnedInventory : IDynamicHashMap<int, byte>
{
    byte IDynamicHashMap<int, byte>.Value { get; }
}
```

| Attribute property | Default |
|---|---|
| `CodecMode` | `GhostDynamicHashMapCodecMode.Generated` |
| `PrefabType` | `GhostPrefabType.All` |
| `SendTypeOptimization` | `GhostSendType.AllClients` |
| `OwnerSendType` | `SendToOwnerType.All` |
| `SendDataForChildEntity` | `false` |

Set `SendDataForChildEntity` to `true` for marker buffers that should serialize on child entities.

The serializer keeps NetCode's outer dynamic-buffer length equal to the physical byte buffer length, but the changed wire payload is:

```text
16-byte compact header + active keys + active values
```

`Buckets`, `Next`, holes, free-list state, unused capacity slots, and unused snapshot scratch bytes are not written to the stream.
An unchanged map writes no payload. Any logical change currently sends the whole compact map payload.

### Stage A Measurements

The compact payload byte count is deterministic:

```text
16 + Count * (EncodedKeySize + EncodedValueSize)
```

For `RawStable`, the encoded sizes are `sizeof(TKey)` and `sizeof(TValue)`. Generated mode uses the sum of its generated field-codec sizes, which can differ from the in-memory struct size because padding is omitted.

Physical-byte replication sends the whole backing byte buffer. Snapshot history still uses the physical dynamic-buffer length so NetCode can resize
the destination byte buffer safely before reconstruction:

```text
aligned(change mask bytes + physical byte buffer length)
```

Wire bytes shrink with active entry count, while snapshot history remains physical-length based until optional length hooks exist.

### Protocol Versioning

The current full wire-format identities are:

- `BovineLabs.Core.Iterators.DynamicHashMapRawCompactPayload.v1`
- `BovineLabs.Core.Iterators.DynamicHashMapGeneratedCompactPayload.v2`
- `BovineLabs.Core.Iterators.DynamicMultiHashMapRawCompactPayload.v1`
- `BovineLabs.Core.Iterators.DynamicMultiHashMapGeneratedCompactPayload.v2`

All use `DynamicHashMapCompactHeader.CurrentFormatVersion == 1`. The serializer's ghost-fields hash includes:

- format name and format version
- key and value type names
- raw encoded key and value sizes, or generated encoded sizes and schema hashes
- codec type name
- collection semantics for multimap variants

Any incompatible change to the compact header, payload layout, codec semantics, key/value encoding, or collection kind must use a new format identity
and produce a different ghost-fields hash. Existing payloads are not migrated in place; rolling out a new format requires the usual NetCode
protocol-version separation between old and new clients.

### Raw Codec Limitations

`GhostDynamicHashMapCodecMode.RawStable` accepts any unmanaged key and value type and copies its complete in-memory bytes. This broad generator acceptance is not a guarantee of network stability: the caller must ensure identical size, layout, padding contents, and meaning across every client, server, platform, and build participating in the protocol. Plain primitives, enums, and deliberately stable unmanaged structs are the usual fits; pointers, allocator-backed containers, and process-local handles are not meaningful wire values even when their containing type is unmanaged.

Use generated mode when the data can be expressed as supported primitives, enums, or recursively supported structs with public writable fields. It does not implement `GhostField` quantization and does not support every unmanaged Unity type; generator diagnostics identify an unsupported type or storage field.

### MultiHashMap Semantics

`IDynamicMultiHashMap<TKey, TValue>` variants preserve duplicate keys, duplicate identical pairs, and per-key iteration order. That ordering is part of
the protocol identity. There is no unordered multimap mode.

## Container-Specific Usage

### UntypedBuffer

`IDynamicUntypedBuffer` stores an ordered sequence of differently sized unmanaged values. The caller supplies the exact type again when reading or replacing an index:

```csharp
public struct MixedValues : IDynamicUntypedBuffer
{
    byte IDynamicUntypedBuffer.Value { get; }
}

var entity = baker.GetEntity(TransformUsageFlags.None);
var buffer = baker.AddBuffer<MixedValues>(entity);
var values = buffer.Initialize().AsMap();

values.Add(7);
values.Add(new float3(1, 2, 3));

var count = values.ElementAtRO<int>(0);
ref readonly var position = ref values.ElementAtRO<float3>(1);
```

The stored type is checked only in collection-checks/debug builds. Keep the type associated with each index in the owning schema; an incorrect `ElementAt*<T>` is unsafe in release code.

### UntypedHashMap
```csharp
public struct ConfigMap : IDynamicUntypedHashMap<FixedString64Bytes>
{
    byte IDynamicUntypedHashMap<FixedString64Bytes>.Value { get; }
}

// Runtime type flexibility
config.Add<float>("speed", 5.0f);
config.Add<int>("lives", 3);
```

### VariableMap
```csharp
using BovineLabs.Core.Iterators.Columns;

// Single column example
public struct InventoryMap : IDynamicVariableMap<int, ItemData, float, OrderedListColumn<float>>
{
    byte IDynamicVariableMap<int, ItemData, float, OrderedListColumn<float>>.Value { get; }
}

// Two column example  
public struct EntityRelations : IDynamicVariableMap<Entity, RelationData, int, OrderedListColumn<int>, float, OrderedListColumn<float>>
{
    byte IDynamicVariableMap<Entity, RelationData, int, OrderedListColumn<int>, float, OrderedListColumn<float>>.Value { get; }
}

// Usage with auto-generated extensions
var entity = baker.GetEntity(TransformUsageFlags.None);
var buffer = baker.AddBuffer<InventoryMap>(entity);
buffer.Initialize(capacity: 64);
var map = buffer.AsMap();

// Operations include column data
map.Add(itemId, itemData, weight);
if (map.TryGetValue(itemId, out var item, out var itemWeight))
{
    // Process item with its weight
}

// Iterate with column access
foreach (var kvc in map)
{
    ProcessItem(kvc.Key, kvc.Value, kvc.Column);
}
```

### PerfectHashMap
```csharp
public struct OptimizedLookup : IDynamicPerfectHashMap<int, float>
{
    byte IDynamicPerfectHashMap<int, float>.Value { get; }
}

// Manual initialization required with data source
var sourceMap = new NativeHashMap<int, float>(10, Allocator.Temp);
sourceMap.Add(1, 1.5f);
sourceMap.Add(5, 2.5f);

var entity = baker.GetEntity(TransformUsageFlags.None);
var buffer = baker.AddBuffer<OptimizedLookup>(entity);
buffer.InitializePerfectHashMap<OptimizedLookup, int, float>(sourceMap, 0f);

// Auto-generated AsMap method available
var lookup = buffer.AsMap();
float value = lookup[1]; // Fast O(1) access
```

`TValue` must implement `IEquatable<TValue>`. The `nullValue` passed during initialization is an empty-slot sentinel and cannot be stored as a present value: `TryGetValue` and `ContainsKey` treat it as absent. The indexer can update an existing key or fill an empty collision-free slot, but the map does not implement enumeration; use `TryGetValue`, `ContainsKey`, or the indexer for lookup access.

## Performance Tips

- **Pre-size containers**: Set appropriate initial capacity to avoid resizes
- **Single-threaded only**: Not write thread-safe, use proper job scheduling

## Editor inspection

`BovineLabs.Core.Editor.Inspectors` exposes entity-inspector elements for the generated byte-backed family:

- `DynamicHashMapElement` combines list and optional search views.
- `DynamicHashMapListElement` edits map key/value pairs.
- `DynamicHashMapSearchElement` selects from supplied search items.
- `DynamicHashSetListElement` edits set values.

Use them from editor-only code referencing `BovineLabs.Core.Editor`. They operate on the currently inspected entity and require the matching initialized buffer.

## Troubleshooting

**`AsMap()` or `Initialize()` is missing**

Make the marker a top-level type, implement the exact `IDynamic*` interface, reference `BovineLabs.Core`, and fix earlier generator/compiler errors.

**Lookup reads invalid data**

The buffer was not initialized, or application code resized/modified the raw byte buffer. Recreate it through the generated initialization API and mutate only through its wrapper.

**A NetCode serializer is not generated**

Install `com.unity.netcode`, verify `UNITY_NETCODE` is active, and use `[GhostDynamicHashMap]` only on `IDynamicHashMap` or `IDynamicMultiHashMap` marker buffers.

**The generated map never appears on the remote ghost**

Add and initialize the attributed marker buffer on the ghost prefab. `[GhostDynamicHashMap]` generates and registers a serializer; it does not attach the buffer to a prefab.

**Concurrent writes fail**

The wrappers are single-writer containers. Serialize writes through ECS dependencies or gather changes into a separate concurrent container.

## Related guides

- [Dynamic buffer collections](DynamicCollections.md)
- [Collections](Collections.md)
- [Iterators](Iterators.md)
- [Inspectors](Inspectors.md)
