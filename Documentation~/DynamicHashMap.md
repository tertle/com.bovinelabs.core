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