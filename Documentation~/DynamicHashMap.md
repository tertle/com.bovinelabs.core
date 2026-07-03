# DynamicHashMap

## Overview

Provides HashMap and other container types for entities by reinterpreting DynamicBuffers as native containers. This enables efficient key-value storage directly on entities with full Burst compatibility.

## Container Types

| Interface                                  | Purpose                       |
|--------------------------------------------|-------------------------------|
| `IDynamicHashMap<TKey, TValue>`            | Standard key-value dictionary |
| `IDynamicMultiHashMap<TKey, TValue>`       | Multiple values per key       |
| `IDynamicHashSet<TKey>`                    | Unique value set              |
| `IDynamicUntypedHashMap<TKey>`             | Variable value types          |
| `IDynamicPerfectHashMap<TKey, TValue>`     | Read-only optimized map       |
| `IDynamicVariableMap<TKey, TValue, T, TC>` | HashMap with extra column/s   |

## Basic Setup

Define your container component:

```csharp
[InternalBufferCapacity(0)]
public struct PlayerInventory : IDynamicHashMap<int, ItemData>
{
    byte IDynamicHashMap<int, ItemData>.Value { get; }
}
```

Initialize in your baker:

```csharp
var buffer = baker.AddBuffer<PlayerInventory>();
buffer.Initialize();
```

## Usage

Access the container in systems:

```csharp
[BurstCompile]
public partial struct InventorySystem : IJobEntity
{
    public void Execute(DynamicBuffer<PlayerInventory> buffer)
    {
        var inventory = buffer.AsMap();
        
        // Basic operations
        inventory.Add(itemId, itemData);
        if (inventory.TryGetValue(itemId, out var item))
        {
            // Process item
        }
        inventory.Remove(itemId);
        
        // Batch operations for performance
        inventory.AddBatchUnsafe(itemIds, itemDataArray);
        
        // Enumeration
        foreach (var kvp in inventory)
        {
            // Process each item
        }
    }
}
```

## Extension Methods

**Auto-generation**: AsMap and Initialize extensions are automatically generated for `HashMap`, `MultiHashMap`, `HashSet`, `UntypedHashMap`, `VariableMap`, and `PerfectHashMap` variants. For `PerfectHashMap`, only `AsMap()` is generated - `Initialize()` methods require manual implementation due to special parameters.

## NetCode Dynamic Hash Map Serialization

When `com.unity.netcode` is installed, `IDynamicHashMap<TKey, TValue>` and `IDynamicMultiHashMap<TKey, TValue>` marker buffers can opt into compact
NetCode serialization with one attribute. The generator infers the collection kind from the implemented interface.

```csharp
[GhostDynamicHashMap(IsDefault = true)]
public struct PlayerInventory : IDynamicHashMap<int, byte>
{
    byte IDynamicHashMap<int, byte>.Value { get; }
}

[GhostDynamicHashMap(IsDefault = true)]
public struct DamageBySource : IDynamicMultiHashMap<int, byte>
{
    byte IDynamicMultiHashMap<int, byte>.Value { get; }
}
```

Generated field encoding is the default. It writes deterministic key/value fields and omits padding from custom structs. Use `RawStable` only when both
key and value have stable raw byte representations:

```csharp
[GhostDynamicHashMap(CodecMode = GhostDynamicHashMapCodecMode.RawStable, IsDefault = true)]
public struct RawInventory : IDynamicHashMap<int, byte>
{
    byte IDynamicHashMap<int, byte>.Value { get; }
}
```

NetCode metadata uses the same public names as `GhostComponentAttribute`:

```csharp
[GhostDynamicHashMap(
    IsDefault = true,
    OwnerSendType = SendToOwnerType.SendToOwner,
    SendDataForChildEntity = true)]
public struct OwnedInventory : IDynamicHashMap<int, byte>
{
    byte IDynamicHashMap<int, byte>.Value { get; }
}
```

`SendDataForChildEntity` defaults to `false`. Set it to `true` for marker buffers that should serialize on child entities.

The serializer keeps NetCode's outer dynamic-buffer length equal to the physical byte buffer length, but the changed wire payload is:

```text
16-byte compact header + active keys + active values
```

`Buckets`, `Next`, holes, free-list state, unused capacity slots, and unused snapshot scratch bytes are not written to the stream.
An unchanged map writes no payload. Any logical change currently sends the whole compact map payload.

### Stage A Measurements

The compact payload byte count is deterministic:

```text
16 + Count * (sizeof(TKey) + sizeof(TValue))
```

Physical-byte replication sends the whole backing byte buffer. Stage A still reserves snapshot history using the physical dynamic-buffer length so
NetCode can resize the destination byte buffer safely before reconstruction:

```text
aligned(change mask bytes + physical byte buffer length)
```

The Core test suite includes `StageA_Measurements_CompareWireSnapshotCpuAndAllocations`, which logs compact payload bytes, physical bytes,
snapshot-history bytes, pack/deserialization/rebuild timings, and steady-state managed allocations for a sparse representative map. That test is the
release gate for the current Stage A tradeoff: wire bytes shrink with active entry count, while snapshot history remains physical-length based until
optional Stage B length hooks exist.

### Protocol Versioning

The current wire formats are:

- `DynamicHashMapRawCompactPayload.v1`
- `DynamicHashMapGeneratedCompactPayload.v2`
- `DynamicMultiHashMapRawCompactPayload.v1`
- `DynamicMultiHashMapGeneratedCompactPayload.v2`

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

`GhostDynamicHashMapCodecMode.RawStable` is deliberately narrow. The source generator accepts only marker key/value types whose
raw byte representation is network-stable:

- primitive numeric types
- `bool`
- `char`
- enums backed by supported primitive types

The raw codec rejects `Entity`, fixed strings, structs, padded custom value types, quantized values, and nested codecs. Use generated mode for those
cases unless a separately versioned raw protocol is introduced.

### MultiHashMap Semantics

`IDynamicMultiHashMap<TKey, TValue>` variants preserve duplicate keys, duplicate identical pairs, and per-key iteration order. That ordering is part of
the protocol identity. There is no unordered multimap mode.

## Container-Specific Usage

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
var buffer = baker.AddBuffer<InventoryMap>();
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

var buffer = baker.AddBuffer<OptimizedLookup>();
buffer.InitializePerfectHashMap<OptimizedLookup, int, float>(sourceMap, 0f);

// Auto-generated AsMap method available
var lookup = buffer.AsMap();
float value = lookup[1]; // Fast O(1) access
```

## Performance Tips

- **Pre-size containers**: Set appropriate initial capacity to avoid resizes
- **Single-threaded only**: Not write thread-safe, use proper job scheduling
