# Utility

Core includes small runtime helpers for math, worlds, unmanaged data, reflection, and editor diagnostics. Most live in `BovineLabs.Core.Utility`; world flags live in `BovineLabs.Core`, and related extension methods live in `BovineLabs.Core.Extensions`.

## Start with the focused guides

| Need | Guide |
|---|---|
| Call managed code synchronously from Burst | [BurstTrampoline](BurstTrampoline.md) |
| Non-deterministic random values in parallel jobs | [GlobalRandom](GlobalRandom.md) |
| Short-lived pooled list scratch | [PooledNativeList](PooledNativeList.md) |
| Frequently rebuilt spatial broad phase | [Spatial](Spatial.md) |
| Native and entity-backed containers | [Collections](Collections.md) |
| Reusable entity construction across bakers/tests/runtime | [EntityCommands](EntityCommands.md) |

## World helpers

### `Worlds`

`BovineLabs.Core.Worlds` defines the project-wide `WorldSystemFilterFlags` combinations used by package systems:

- `ClientLocal`, `ServerLocal`, and `ServerLocalEditor`
- `Simulation`, `SimulationService`, `SimulationMenu`, and editor combinations
- custom `Service` and `Menu` filter bits
- `All`
- `ServiceWorld` and `MenuWorld` `WorldFlags` values for constructing those worlds

```csharp
[WorldSystemFilter(Worlds.ServerLocal, Worlds.ServerLocal)]
public partial struct AuthoritativeSystem : ISystem
{
}
```

`Worlds.IsServiceWorld(...)` and `Worlds.IsLocalWorld(...)` work with managed and unmanaged worlds. `WorldExtensions` adds `IsClientWorld`, `IsServerWorld`, `IsThinClientWorld`, `IsServerLocalWorld`, and `IsEditorWorld` overloads.

Keep filter flags and instance flags distinct: `WorldSystemFilterFlags` select which systems belong in a world, while `WorldFlags` describe an existing world.

### `WorldUtility`

`WorldUtility.AllExcludingAdvanced()` enumerates the currently registered worlds whose flags include `WorldFlags.Live`.

### `InitSystemBase`

`InitSystemBase` is a managed `SystemBase` placed first in `InitializationSystemGroup`. Its default `OnUpdate` removes the system from the group's update list, which suits subclasses that perform one-time work in `OnCreate`. A subclass that overrides `OnUpdate` replaces that default removal behavior unless it calls the base implementation.

## ECS helpers

### `TransformUtility.SetupLocalToWorld`

`SetupLocalToWorld(...)` recomputes existing `LocalToWorld` values for a `LinkedEntityGroup` from `LocalTransform`, `Parent`, and optional `PostTransformMatrix` data. Use it when a linked hierarchy must be synchronized immediately rather than waiting for the normal transform systems.

Important constraints:

- It sets existing components; it does not add missing transform components.
- Linked entities without `LocalToWorld` are skipped.
- A missing `LocalTransform` in a traversed parent chain throws.
- Parent cycles are rejected after a bounded traversal.
- The overload taking explicit lookups is preferable when the caller already owns and updates them.

### `QueryEntityEnumerator`

`QueryEntityEnumerator` exposes low-level chunk iteration for an `EntityQuery`, including enabled masks through `ChunkEntityEnumerator`. Prefer Unity's normal query/job APIs unless this manual iteration is required. The caller owns dependency completion and must not perform structural changes while using the raw iteration state.

### Query groups

`[QueryGroup(typeof(Target))]` in `BovineLabs.Core.Utility` declares a component or buffer as an override of `Target`. Multiple targets are supported and duplicate declarations are deduplicated. BovineLabs library queries use these relationships independently of Unity's deprecated write groups.

`TypeManagerEx` discovers registered component attributes once on code load and releases its native metadata on code unload. Its main-thread `Initialize()` is idempotent. Register closed generic components with Entities before initialization; types registered afterward are outside this snapshot. `GetQueryGroupCount(TypeIndex)` and the borrowed `GetQueryGroups(TypeIndex)` pointer are Burst-compatible; the pointer must not be retained across code unloading. No debug-name cache is created.

`TypeManagerUtil.GetQueryGroupComponents<T>(allocator)` returns an owned array of read-only direct group members. The `ComponentType` overload accepts a runtime target; another overload splits enableable and ordinary members. Dispose owned arrays according to their allocator lifetime.

The extensions in `BovineLabs.Core.Extensions` provide:

- `WithAnyQueryGroup<T>()` / `WithNoneQueryGroup<T>()`: add direct members to `Any` / `None` regardless of the target's access mode. Both also accept `ComponentType`. An empty group adds no constraints, including for `WithAnyQueryGroup`.
- `WithQueryGroupFilter()`: reproduces the former `FilterWriteGroup` rules using the new attribute. Unmentioned direct members of writable targets are added to `None`. Explicit constraints and transitive members of read-only targets are permitted. `All`, `Any`, `Disabled`, and `Present` targets are considered; `None` and `Absent` also count as explicit mentions. Call this after all constraints for each query description, before `Build` or `AddAdditionalQuery`; it applies immediately, so later constraints are not considered.

`QueryGroupMatcher<T>` matches direct members in a chunk, including enabled bits for enableable components and buffers. Missing members do not match. Ordinary members match the entire chunk, including prefab and disabled-entity archetypes; query selection controls which chunks are inspected. The owner must complete readers and dispose its persistent array.

## Math and geometry

### `mathex`

Frequently useful operations include:

- vectorized `min`, `max`, `sum`, and `minMax` over arrays/pointers;
- angle helpers such as `SmoothDampAngle`, `LerpAngle`, and `DeltaAngle`;
- `Approximately` overloads and 2D/3D rotation helpers;
- `GenerateGaussianNoise`, `NormalDistribution`, and `GammaDistribution` using an explicit `Unity.Mathematics.Random` state;
- `FromToRotation` and perpendicular-vector helpers; and
- constants including `Radians90`, `Radians180`, and `Radians360`.

The lowercase aggregate methods are pointer-oriented Burst utilities; validate array lifetime and pass correct lengths.

### Other geometry helpers

- `IntersectionTests.AABBTriangle(...)` tests an AABB against a triangle.
- `PolygonUtility` calculates signed area and clockwise/counter-clockwise orientation for `NativeArray<float2>` and `NativeArray<float3>` polygons.
- `HalfSizeTriangleMatrix.GetIndex(...)` packs symmetric matrix coordinates into one triangular array.
- `CurveRemapUtility.TryRemapToClipLength(...)` remaps clamp-wrapped `AnimationCurve` keys into clip-local time while preserving tangents.
- `HSV` clamps hue/saturation/value inputs and converts them to `Color` with `ToColor()`.

### Mesh utilities

`ConvexHullBuilder`, `MeshSimplifier`, and `TerrainToMesh` are advanced mesh-processing helpers. `TerrainToMesh` is available only when the Terrain module integration is compiled. Their operations allocate result and working containers; use the requested allocator and dispose returned owning values according to each API.

## Memory and synchronization

### `NoAllocHelpers`

`ExtractArrayFromList(...)` and `ResizeList(...)` access the private layout of managed `List<T>` to avoid normal copying/filling behavior. They are runtime-layout-sensitive unsafe optimizations. Prefer ordinary `List<T>` APIs unless profiling justifies the dependency on internal layout.

### `EntityLock` and `SpinLock`

These are low-level spin-based synchronization tools. Keep critical sections short and do not use them for work that can block or wait on jobs. Wrap the token returned by `EntityLock.Acquire(entity)` in `using`; the `EntityLock` owner must dispose its allocator-backed storage after all users finish. `SpinLock` has no token: pair `Acquire` or a successful `TryAcquire` with `Release`, normally through `try`/`finally`.

## Reflection and type discovery

`ReflectionUtility` caches loaded assemblies, types, methods, implementation searches, and attribute searches. `TypeUtility` checks inheritance against open generic base types and extracts their type arguments.

These are managed reflection APIs, not Burst/job utilities. Discovery can still be expensive on first use; cache domain-specific results instead of repeatedly filtering the global type set in hot paths.

## Raw serialization and compression

### `Serializer` and `Deserializer`

These types append and read unmanaged values from raw byte storage. `Serializer` owns an `UnsafeList<byte>` and must be disposed. `Deserializer` borrows a `NativeArray<byte>` or external pointer and advances `CurrentIndex` as values are read.

They do not define a versioned save format, perform schema migration, normalize endianness, or add comprehensive bounds validation. The caller must define the layout, validate input length, and keep borrowed memory alive.

### `CodecService`

`CodecService` currently exposes LZ4 compression through `Codec.LZ4`. Use `GetBoundedSize` when supplying a destination buffer. The overload that allocates an output pointer transfers its allocator lifetime to the caller; release owned non-temporary allocations with the same allocator. Decompression requires the exact expected uncompressed size and returns false when LZ4 produces a different number of bytes.

## Diagnostics and command-line helpers

### `TimeProfiler`

`TimeProfiler` is an editor-only scoped timer gated by `BLLogger.Level`. Use `Start`, `StartWithMin`, or their string variants in a `using` scope. Player builds return a no-op value.

### `CommandLineArgs`

`TryGetArgument` expects separate option and value tokens:

```text
-app.target-frame-rate 60
```

`Contains` checks for a standalone option. Arguments are captured from `Environment.GetCommandLineArgs()` when the type initializes.

### `BurstUtil`

- `IsEmpty(ref EntityQuery)` provides a Burst-compiled query emptiness call.
- `SetNotBurstCompiled(ref bool)` is discarded by Burst and can detect whether a path is running without Burst.

For assertions and logging, see [Debug](Debug.md).
