# Iterators

BovineLabs Core provides comprehensive iterator utilities designed for high-performance ECS applications, offering Burst-compatible enumeration capabilities across various data structures optimized for Unity DOTS.

## Dynamic HashMap Iterators

The iterators for DynamicHashMap collections are part of the [DynamicHashMap](DynamicHashMap.md) system. For detailed information about DynamicHashMap usage, iteration patterns, and performance characteristics, see the [DynamicHashMap documentation](DynamicHashMap.md).

### Key Iterator Types
- **`DynamicHashMapEnumerator<TKey, TValue>`** - Standard key-value pair enumeration
- **`DynamicHashMapKeyEnumerator<TKey, TValue>`** - Multi-hash map value enumeration for specific keys
- **`DynamicHashSetEnumerator<T>`** - Hash set enumeration (unique values only)
- **`UntypedDynamicHashMapIterator`** - Type-unsafe iteration using `IntPtr`

All DynamicHashMap iterators are Burst-compatible and integrate seamlessly with the `DynamicBuffer<byte>` backing storage system.

## Blob Collection Iterators

### `BlobHashMapEnumerator<TKey, TValue>`
Enumerator for blob-based hash maps optimized for blob asset storage.
- Read-only enumeration with `[NativeContainerIsReadOnly]`
- Efficient bucket-based iteration
- Integrated with Unity's blob asset system

### `BlobMultiHashMapIterator<TKey>`
Iterator for blob-based multi-hash maps with multiple values per key.
- Optimized for blob storage patterns
- Supports multi-value enumeration

## Entity Query Iterators

### `QueryEntityEnumerator`
High-performance entity query iterator with chunk-based processing.
- Optimized for ECS chunk iteration
- Supports enabled component masks
- Integrates with Unity's native query system

### `ChunkEntityEnumerator`
Per-chunk entity enumeration with enabled mask support.
- Handles sparse and dense entity scenarios
- Efficient bit manipulation for enabled components
- Cache-friendly iteration patterns

## Custom Chunk Iterator

### `CustomChunkIterator<T>`
Generic chunk iteration with custom execution logic.
- Burst-compatible chunk processing
- Automatic enabled mask handling
- Optimized for both sparse and dense entity scenarios

### `ICustomChunkIterator`
Interface for custom chunk iteration implementations.
- Extensible iteration patterns
- Supports custom processing logic
- Enables advanced iteration scenarios

## Lookup Utilities

### `UnsafeComponentLookup<T>`
Direct component access by entity with maximum performance.
- Unsafe direct memory access
- Cached archetype lookups for efficiency
- Support for both read and write operations

### `UnsafeBufferLookup<T>`
Direct buffer access by entity for dynamic buffer operations.
- High-performance buffer access
- Minimal overhead for buffer operations

### `ChangeFilterLookup<T>`
Change detection and filtering during iteration.
- Efficient change detection
- Version tracking for optimization
- Critical for performance in ECS systems

### `SharedComponentLookup<T>`
Shared component access across multiple entities.
- Optimized for shared data patterns
- Reduced memory footprint

### `UnsafeEnableableLookup<T>`
Enableable component state management during iteration.
- Efficient enabled/disabled state checking
- Supports conditional processing

## Key Design Patterns

### KVPair Structure
Custom `KVPair<TKey, TValue>` structure providing:
- Reference-based value access
- Efficient memory layout
- Debugger display attributes
- Null pattern support

### Native Container Integration
All iterators follow Unity's Native Container patterns:
- `[NativeContainer]` attributes for safety system integration
- `[NativeContainerIsReadOnly]` for read-only access optimization
- `[NativeDisableUnsafePtrRestriction]` for performance-critical unsafe operations

### Burst Compatibility
All iterators are designed to be Burst-compatible:
- No managed allocations during iteration
- Aggressive inlining with `[MethodImpl(MethodImplOptions.AggressiveInlining)]`
- Unsafe pointer operations where performance is critical

## Performance Optimizations

### Enabled Mask Optimization
The `CustomChunkIterator` uses sophisticated enabled mask handling:
- Automatic detection of sparse vs dense scenarios
- Range-based iteration for sparse data
- Bit manipulation for dense data
- Edge count optimization for boundary cases

### Cache-Friendly Iteration
- Archetype caching in lookup utilities
- Bucket-based iteration for hash maps
- Chunk-based entity processing for memory locality

### Minimal Allocation
- Struct-based implementations
- Reuse of iterator state
- Pooled temporary containers

## Usage Patterns

### Dynamic HashMap Enumeration
```csharp
// Standard key-value enumeration
foreach (var kvp in dynamicHashMap)
{
    // Access kvp.Key and kvp.Value
}

// Multi-hash map key enumeration
foreach (var value in multiHashMap.GetValues(key))
{
    // Process each value for the key
}
```

### Entity Query Enumeration
```csharp
var queryEnumerator = new QueryEntityEnumerator(query);
while (queryEnumerator.MoveNextChunk(out var chunk, out var entityEnumerator))
{
    while (entityEnumerator.NextEntityIndex(out var entityIndex))
    {
        // Process entity at entityIndex
    }
}
```

### Custom Chunk Processing
```csharp
var iterator = new CustomChunkIterator<MyComponent>();
iterator.Execute(query, (ref MyComponent component, int entityIndex) =>
{
    // Process component for entity at entityIndex
});
```

## Integration with Dynamic Collections

The iterators are tightly integrated with the package's dynamic collection system:
- **DynamicHashMap**: Uses `DynamicBuffer<byte>` as backing storage
- **Automatic Code Generation**: Extensions are generated for common usage patterns
- **Type Safety**: Strong typing while maintaining performance characteristics