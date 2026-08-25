# Spatial maps

The spatial maps rebuild a broad-phase lookup from a flat array of positions. They store the index of each input element, so a query can use the same index to read a parallel entity or component array.

```csharp
using BovineLabs.Core.Spatial;
```

## Choosing a map

| Requirement | Type | Query shape |
|---|---|---|
| Simple rectangular cell scan | `SpatialMap<T>` | Quantized square-grid bounds |
| Circular searches with less corner overdraw | `SpatialHexMap<T>` | Axial hex rings |
| Full 3D cell scan | `SpatialMap3<T>` | Quantized cubic-grid bounds |
| Dense integer-key buckets with native safety | `SpatialKeyedMap<T>` | Quantized square-grid bounds |
| Borrow and return position values directly | `LocalSpatialMap<T>` | Quantized square-grid bounds |

These maps are intended for data that changes often and can be rebuilt before their readers run. They are broad-phase structures: always perform the exact distance or overlap test after retrieving candidates.

`T` must be unmanaged and implement `ISpatialPosition`:

```csharp
public interface ISpatialPosition
{
    float2 Position { get; }
}
```

Core's `SpatialPosition` stores a `float3` and exposes its `xz` coordinates through `ISpatialPosition`.

## Construction and ownership

The constructor order is `quantizeStep`, then `size`:

```csharp
var square = new SpatialMap<SpatialPosition>(quantizeStep: 16, size: 4096);
var hex = new SpatialHexMap<SpatialPosition>(quantizeStep: 16, size: 4096);
```

- `size` is the width and height of a square world centered on the origin.
- `quantizeStep` controls cell spacing. Smaller cells reduce candidates per cell but increase the number of cells a large query must visit.
- The optional allocator defaults to `Allocator.Persistent`. The owner must call `Dispose()` after all map jobs complete.
- Memory is driven primarily by the number of input positions and the native hash-map capacity. The configured world is not allocated as a dense `size / quantizeStep` grid.
- Input positions must remain inside the configured world. Out-of-bounds positions are unsupported; checks/debug builds diagnose relevant bounds failures.
- Do not rebuild or dispose a map while jobs are still reading a previous build. Chain every build and reader through the owning system dependency.

`Build(...)` accepts either `NativeArray<T>` or `NativeList<T>` and returns the handle for the resize, quantize, and bucket-rebuild jobs:

```csharp
state.Dependency = this.spatialMap.Build(positions, state.Dependency);
```

The positions array and every parallel array indexed through the map must stay valid until all readers complete.

## Other map variants

`SpatialMap3<T>` is the three-dimensional counterpart to `SpatialMap<T>`. Its element type implements `ISpatialPosition3`, its configured `size` describes a cube centered on the origin, and its read-only view quantizes `float3` cells into `long` hashes. It retains the input-index lookup model and input-count-driven hash-map capacity.

`SpatialKeyedMap<T>` uses a `NativeKeyedMap<int>` instead of `NativeParallelMultiHashMap<int, int>`. It still returns input indices, but it allocates a direct bucket for every possible square-grid key. Its bucket memory therefore scales with `ceil(size / quantizeStep)^2`; use it only when that bounded grid is reasonably sized and direct integer-key buckets are desirable.

`LocalSpatialMap<T>` uses an unsafe partial keyed map and returns the `T` values themselves. It borrows the positions storage passed to `Build` rather than copying the values, so that array or deferred list storage must not move or expire until every reader completes. Its bucket memory also scales with the square-grid cell count, it does not provide native-container safety handles, and it has only immediate `Dispose()`; complete its jobs before disposal.

## Gathering `LocalTransform` positions

`PositionBuilder` gathers `LocalTransform.Position` values in an `EntityQuery`'s base-entity-index order:

```csharp
private const int WorldSize = 4096;
private const float QuantizeStep = 16;

private EntityQuery query;
private PositionBuilder positionBuilder;
private SpatialMap<SpatialPosition> spatialMap;
private int quantizedSize;

public void OnCreate(ref SystemState state)
{
    this.query = new EntityQueryBuilder(Allocator.Temp)
        .WithAll<LocalTransform>()
        .Build(ref state);

    this.positionBuilder = new PositionBuilder(ref state, this.query);
    this.spatialMap = new SpatialMap<SpatialPosition>(QuantizeStep, WorldSize);
    this.quantizedSize = (int)math.ceil(WorldSize / QuantizeStep);
}

public void OnDestroy(ref SystemState state)
{
    state.Dependency.Complete();
    this.spatialMap.Dispose();
}

public void OnUpdate(ref SystemState state)
{
    state.Dependency = this.positionBuilder.Gather(ref state, state.Dependency, out NativeArray<SpatialPosition> positions);
    var entities = this.query.ToEntityListAsync(state.WorldUpdateAllocator, state.Dependency, out var entityDependency);
    state.Dependency = this.spatialMap.Build(positions, entityDependency);

    state.Dependency = new FindNeighboursJob
    {
        Entities = entities.AsDeferredJobArray(),
        Positions = positions,
        Map = this.spatialMap.AsReadOnly(),
        QuantizedSize = this.quantizedSize,
    }.ScheduleParallel(state.Dependency);
}
```

The query must not require enableable-component filtering; `PositionBuilder` asserts that no enabled mask is in use. Its output comes from the world's rewindable allocator and is temporary. Do not retain it across allocator rewinds. When candidates must resolve back to entities, gather them from the same query as shown so the parallel indices remain aligned.

## Square-grid example

`SpatialMap<T>.AsReadOnly()` returns `SpatialMap.ReadOnly`. Quantize a search AABB, clamp it to the configured grid, visit the cells, and then apply the exact test:

```csharp
[BurstCompile]
private partial struct FindNeighboursJob : IJobEntity
{
    private const float Radius = 10;

    [ReadOnly]
    public NativeArray<Entity> Entities;

    [ReadOnly]
    public NativeArray<SpatialPosition> Positions;

    [ReadOnly]
    public SpatialMap.ReadOnly Map;

    public int QuantizedSize;

    private void Execute(Entity entity, in LocalTransform transform, DynamicBuffer<Neighbour> neighbours)
    {
        neighbours.Clear();

        var lower = new int2(0);
        var upper = new int2(this.QuantizedSize - 1);
        var min = math.clamp(this.Map.Quantized(transform.Position.xz - Radius), lower, upper);
        var max = math.clamp(this.Map.Quantized(transform.Position.xz + Radius), lower, upper);
        var radiusSq = Radius * Radius;

        for (var y = min.y; y <= max.y; y++)
        {
            for (var x = min.x; x <= max.x; x++)
            {
                var hash = this.Map.Hash(new int2(x, y));
                if (!this.Map.Map.TryGetFirstValue(hash, out var item, out var iterator))
                {
                    continue;
                }

                do
                {
                    var otherEntity = this.Entities[item];
                    if (otherEntity == entity)
                    {
                        continue;
                    }

                    var otherPosition = this.Positions[item].Position;
                    if (math.distancesq(transform.Position.xz, otherPosition.xz) <= radiusSq)
                    {
                        neighbours.Add(new Neighbour { Entity = otherEntity });
                    }
                }
                while (this.Map.Map.TryGetNextValue(out item, ref iterator));
            }
        }
    }
}

public struct Neighbour : IBufferElementData
{
    public Entity Entity;
}
```

Compute `QuantizedSize` with `(int)math.ceil(size / quantizeStep)` when the map is created. Clamping is important: hashing an out-of-grid square cell can alias a valid linearized cell.

## Hex-grid queries

The current hex types are `SpatialHexMap<T>`, the static `SpatialHexMap` helper, and `SpatialHexMap.ReadOnly`.

For a hex map, `quantizeStep` is the horizontal center-to-center distance between adjacent cells. The implementation derives `outerRadius = quantizeStep / sqrt(3)` and computes conservative axial bounds around the configured square world.

Use ring traversal for radius searches:

1. Quantize the query position.
2. Process the center cell.
3. Get a conservative ring count with `SearchRange(radius)`.
4. Walk each ring with `Direction(side)`.
5. Reject cells outside the configured bounds.
6. Use `CellMinDistanceSq(...)` to reject cells that cannot intersect the search circle.
7. Apply the exact candidate distance test.

```csharp
[BurstCompile]
private struct HexQueryJob : IJob
{
    public float2 Position;
    public float Radius;

    [ReadOnly]
    public NativeArray<SpatialPosition> Positions;

    [ReadOnly]
    public SpatialHexMap.ReadOnly Map;

    public void Execute()
    {
        var center = this.Map.Quantized(this.Position);
        var radiusSq = this.Radius * this.Radius;

        this.ProcessCell(center, radiusSq);

        var ringCount = this.Map.SearchRange(this.Radius);
        for (var ring = 1; ring <= ringCount; ring++)
        {
            var cell = center + (this.Map.Direction(4) * ring);

            for (var side = 0; side < 6; side++)
            {
                var direction = this.Map.Direction(side);
                for (var step = 0; step < ring; step++)
                {
                    this.ProcessCell(cell, radiusSq);
                    cell += direction;
                }
            }
        }
    }

    private void ProcessCell(int2 cell, float radiusSq)
    {
        if (!this.Map.IsWithinBounds(cell) || this.Map.CellMinDistanceSq(this.Position, cell) > radiusSq)
        {
            return;
        }

        var hash = this.Map.Hash(cell);
        if (!this.Map.Map.TryGetFirstValue(hash, out var item, out var iterator))
        {
            return;
        }

        do
        {
            if (math.distancesq(this.Position, this.Positions[item].Position.xz) <= radiusSq)
            {
                // Process candidate index `item`.
            }
        }
        while (this.Map.Map.TryGetNextValue(out item, ref iterator));
    }
}
```

The static helper also exposes `Quantized`, `Center`, `Hash`, `Direction`, `SearchRange`, `IsWithinBounds`, and `CellMinDistanceSq` overloads when a map instance is not available.

## Related guides

- [Collections](Collections.md) covers the native containers used alongside the maps.
- [Jobs](Jobs.md) covers the package's custom scheduling helpers.
