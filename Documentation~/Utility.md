# Utility

BovineLabs Core provides a comprehensive collection of utility classes and helpers designed for high-performance Unity DOTS development, covering mathematical operations, memory management, ECS patterns, and development tools.

## Mathematical & Computation Utilities

### `mathex`
Extended mathematical operations with vectorized implementations and SIMD optimizations.
- SIMD-optimized min/max/sum operations for arrays
- Quaternion to Euler conversion with multiple rotation orders
- Angle interpolation (`SmoothDampAngle`, `LerpAngle`, `DeltaAngle`)
- Statistical distributions (normal, gamma, Gaussian noise generation)
- Mathematical constants (`Radians90`, `Radians180`)

### `IntersectionTests`
High-performance geometric intersection algorithms.
- `AABBTriangle()` - AABB vs triangle intersection testing

### `PolygonUtility`
Polygon processing utilities for geometric calculations.
- `SignedArea()` - Calculate signed area of 2D/3D polygons
- Clockwise/counter-clockwise detection methods

### `HalfSizeTriangleMatrix`
Memory-efficient triangular matrix operations.
- `GetIndex()` - Convert 2D coordinates to 1D triangle matrix index

## Memory Management & Performance

### `PooledNativeList<T>`
High-performance pooled native containers with thread-safe object pooling.
- Reduces allocation overhead in hot paths
- Automatic capacity management
- Thread-safe implementation

### `NoAllocHelpers`
Zero-allocation collection utilities for performance-critical scenarios.
- `ExtractArrayFromList()` - Access internal array of List<T>
- `ResizeList()` - Resize lists without allocations

### `WorldAllocator`
World-specific memory allocation management.
- Per-world allocator management
- Integration with BovineLabsBootstrap
- Automatic cleanup on world destruction

## Threading & Synchronization

### `EntityLock`
Entity-specific locking mechanism with reference counting.
- Thread-safe entity locking
- Optimized for high-contention scenarios
- Disposable lock pattern

### `SpinLock`
Low-level spinlock implementation for short-duration locks.
- `Acquire()`, `TryAcquire()`, `Release()` methods
- Optimized for minimal overhead

## ECS & Entity Utilities

### `QueryEntityEnumerator`
Advanced entity query iteration with chunk-based enumeration.
- Enabled component mask support
- Performance-optimized iteration patterns

### `TransformUtility`
Transform hierarchy utilities for parent-child relationships.
- `SetupParent()` - Establish parent-child relationships
- Handles LocalToWorld calculations and Child buffer management
- `SetupLocalToWorld()` - Compute and set LocalToWorld for all entities in a LinkedEntityGroup

### `WorldUtility`
World management helpers for world filtering.
- `AllExcludingAdvanced()` - Filter worlds by live flags

## Reflection & Type Utilities

### `ReflectionUtility`
Comprehensive reflection helpers with performance caching.
- `GetAllImplementations<T>()` - Find all types implementing an interface
- `GetAllWithAttribute<T>()` - Find types with specific attributes
- `GetMethodsWithAttribute<T>()` - Find methods with attributes
- Assembly filtering and caching for performance

### `TypeUtility`
Type system utilities for generic type operations.
- `MatchesOpenGeneric()` - Check if type matches open generic
- `GetOpenGenericArgumentType()` - Extract generic argument types

## System Architecture

### `InitSystemBase`
Base class for initialization systems that run once.
- Automatically removes itself from update list after first run
- Ordered first in InitializationSystemGroup

### `BovineLabsBootstrap`
Application bootstrap framework for service and game world management.
- Configurable frame rate and fixed update settings
- NetCode integration support

### `Worlds`
World system filter flags with predefined combinations.
- Service world, client/server world configurations

## Data Structures & Containers

### `BoolExtensions`
Boolean utility extensions for branchless algorithms.
- `AsByte()` - Convert bool to byte for performance

### `IntFloatUnion`
Type-safe union for int/float conversion with memory layout optimization.

### `ShortHalfUnion`
Union for short/half float conversion.

## Development & Debug Utilities

### `TimeProfiler`
Performance profiling utility for editor-only timing measurements.
- Automatic disposal pattern
- Configurable log levels

### `CommandLineArgs`
Command line argument parsing utilities.
- `TryGetArgument()` - Extract command line values
- `Contains()` - Check for argument existence

### `BurstUtil`
Burst compiler utilities for development.
- `IsEmpty()` - Burst-compatible EntityQuery.IsEmpty
- `SetNotBurstCompiled()` - Debug helper for burst compilation detection

## Mesh & Geometry Processing

### `ConvexHullBuilder`
3D convex hull generation with Quickhull algorithm implementation.
- Burst-compiled for performance
- Mesh export functionality

### `MeshSimplifier`
Mesh simplification algorithms for level-of-detail systems.

### `TerrainToMesh`
Terrain to mesh conversion utilities.

## Serialization & Data

### `Serializer` / `Deserializer`
Data serialization utilities for save/load systems.

### `CodecService`
Encoding/decoding services for data processing.

## Application & Runtime Utilities

### `HSV`
HSV color space utilities for color manipulation.
- `ToColor()` - Convert HSV to Unity Color
- Clamped value validation
