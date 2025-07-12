# Collections

BovineLabs Core provides specialized collection types that extend Unity's native collections with performance optimizations, thread safety, and specialized functionality for ECS development.

## Fixed-Size Collections

### `FixedArray<T, TS>`
Stack-allocated array with compile-time size based on the storage type `TS`.

### `FixedBitMask<T>`
Fixed-size bit mask for efficient boolean operations on a set of values.

### `FixedHashMap<TKey, TValue, TS>`
Stack-allocated hash map with compile-time capacity.

## Specialized Strings

### `MiniString`
Compact 16-byte string optimized for small text storage in components.

## Keyed Maps

### `NativeKeyedMap<TValue>`
Hash map optimized for integer keys with known maximum value.

### `UnsafeKeyedMap<TValue>`
Unsafe version of `NativeKeyedMap` for performance-critical scenarios.

### `NativePartialKeyedMap<TValue>`
Keyed map that supports partial key matching.

### `UnsafePartialKeyedMap<TValue>`
Unsafe version of `NativePartialKeyedMap`.

### `NativePerfectHashMap<TKey, TValue>`
Hash map with perfect hashing for scenarios with known key sets.

### `UnsafePerfectHashMap<TKey, TValue>`
Unsafe version of `NativePerfectHashMap`.

## Thread-Safe Collections

### `ThreadList`
Thread-local list storage for parallel job execution.

### `ThreadRandom`
Thread-local random number generation for parallel jobs.

## Unsafe Collections

### `UnsafeArray<T>`
Unsafe version of `NativeArray` with direct memory access.

### `UnsafeDynamicBuffer<T>`
Unsafe version of `DynamicBuffer` for performance-critical operations.

### `UnsafeUntypedDynamicBuffer`
Untyped dynamic buffer for generic data manipulation.

### `UnsafeUntypedDynamicBufferAccessor`
Accessor for untyped dynamic buffers.

## Specialized Hash Maps

### `NativeParallelMultiHashMapFallback<TKey, TValue>`
Multi-hash map with fallback mechanism for handling capacity overflows.

### `NativeMultiHashMap<TKey, TValue>`
Enhanced multi-hash map with additional functionality.

### `UnsafeMultiHashMap<TKey, TValue>`
Unsafe version of multi-hash map.

## Work Processing

### `NativeWorkQueue<T>`
Thread-safe work queue for parallel job processing.

### `NativeLinearCongruentialGenerator`
Fast random number generator optimized for parallel execution.

## Pooling Systems

### `UnmanagedPool<T>`
Memory pool for unmanaged types.

## Utility Collections

### `BitArray`
Efficient bit manipulation and storage.

### `NativeCounter`
Thread-safe counter for parallel operations.

### `Reference<T>`
Managed reference wrapper for ECS components.

### `UntypedDynamicBuffer`
Dynamic buffer that can store any unmanaged type.

## Dynamic Buffer Extensions

### `DynamicBufferAccessor<T>`
Enhanced accessor for dynamic buffers with additional functionality.

### `UnsafeHashMapBucketData<TKey, TValue>`
Low-level hash map bucket data structure.

## Event Processing

### `EventStream<T>`
Stream-based event processing system (see EventStream subfolder).