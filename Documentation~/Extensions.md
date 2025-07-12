# Extensions

BovineLabs Core provides extensive extension methods that enhance Unity's DOTS APIs with performance optimizations, convenience methods, and advanced functionality for ECS development.

## Entity & Component Extensions

### `EntityManagerExtensions`
Enhanced entity manager operations for advanced entity manipulation.
- `GetChunkBuffer<T>()` - Access chunk-level buffers
- `GetUntypedBuffer()` - Get untyped dynamic buffers
- `NumberOfArchetype()` - Get archetype count information

### `ComponentLookupExtensions`
Optimized component lookup operations with performance enhancements.
- `GetOptionalComponentDataRW<T>()` - Get writable component data with null safety
- `GetRefRWNoChangeFilter<T>()` - Get reference without triggering change filters

### `BufferLookupExtensions`
Enhanced buffer lookup operations for dynamic buffer access.

### `SystemStateExtensions`
Extended system state functionality for advanced system operations.
- `GetUnsafeComponentLookup<T>()` - Get unsafe component lookups
- `GetSharedComponentLookup<T>()` - Get shared component lookups
- `GetUnsafeEntityDataAccess()` - Direct entity data access

## Query & Archetype Extensions

### `EntityQueryExtensions`
Advanced query operations and manipulation.
- `QueryHasSharedFilter<T>()` - Check for shared component filters
- `ReplaceSharedComponentFilter<T>()` - Replace shared component filters dynamically

### `EntityQueryBuilderExtensions`
Enhanced query building with additional functionality.

### `ArchetypeChunkExtensions`
Optimized chunk operations for performance-critical scenarios.

## Collection Extensions

### `NativeArrayExtensions`
Enhanced array operations with performance optimizations.
- `ElementAt<T>()` - Get reference to array element
- `ElementAtAsPtr<T>()` - Get pointer to array element
- Predicate and selector interfaces for functional operations

### `NativeListExtensions`
List manipulation utilities with enhanced functionality.

### `DynamicBufferExtensions`
Buffer operations with performance and convenience enhancements.
- `ResizeInitialized<T>()` - Resize buffer and clear memory
- `AddRange<T>()` - Add multiple elements efficiently
- `InsertAllocate<T>()` - Insert and allocate space in one operation
- `AsNativeArrayRO<T>()` - Get read-only native array view

### `UnsafeListExtensions`
Performance-critical list operations using unsafe code.

### Native Collection Extensions
Extensions for Unity's native collections:
- `NativeHashMapExtensions` - Enhanced hash map operations
- `NativeHashSetExtensions` - Set manipulation utilities
- `NativeParallelHashMapExtensions` - Parallel hash map operations
- `NativeParallelMultiHashMapExtensions` - Multi-hash map utilities
- `NativeSliceExtensions` - Slice manipulation
- `NativeStreamExtensions` - Stream processing utilities

## System & World Extensions

### `WorldExtensions`
World type checking and identification.
- `IsClientWorld()` - Check if world is client (includes thin clients)
- `IsServerWorld()` - Check if world is server
- `IsThinClientWorld()` - Check if world is thin client

### `ComponentSystemBaseExtensions`
System utilities for component system operations.

### `EntityCommandBufferExtensions`
Enhanced command buffer operations for batch entity operations.

## Mathematics & Utility Extensions

### `MathematicsExtensions`
Extended math operations beyond Unity.Mathematics.
- `Encapsulate()` - Combine AABBs
- `Expand()` - Expand AABB size
- `IsDefault()` - Check if AABB is default
- `Right()`, `Up()`, `Forward()` - Extract vectors from transformation matrices

### `PhysicsExtensions`
Physics-related utilities for physics simulations.

### `StringExtensions`
String manipulation helpers for text processing.

### `GameObjectExtensions`
GameObject utilities for authoring workflows.

## Performance & Unsafe Extensions

### `UnsafeParallelHashMapExtensions`
High-performance unsafe hash map operations for critical paths.

### `UnsafeParallelMultiHashMapExtensions`
Unsafe multi-hash map operations for maximum performance.

### `UnsafeHashMapExtensions`
Unsafe hash map utilities for performance-critical scenarios.

### `RefRWExtensions`
Reference handling utilities for component access.

### `EntityDataAccessExtensions`
Direct entity data access for advanced operations.

## Specialized Extensions

### `EntityStorageInfoLookupExtensions`
Entity storage information utilities.

### `EntitySceneReferenceExtensions`
Scene reference manipulation for SubScene workflows.

### `MinMaxAABBExtensions`
AABB manipulation utilities.

### `RayExtension`
Ray-related utilities for spatial operations.

### `KVPairExtensions`
Key-value pair utilities for data structures.

## Container Management

### `ContainerClearJobs`
Job-based container clearing operations.

### `NativeHashMapFactory`
Factory methods for creating native hash maps.

### `NativeHashSetFactory`
Factory methods for creating native hash sets.