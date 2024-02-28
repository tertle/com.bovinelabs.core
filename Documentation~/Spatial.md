# Spatial
## Summary
Spatial provides a very fast to generate spatial hashmap designed to be used when it needs to be rebuilt every frame.
It is extremely fast to build while still providing excellent speeds for finding nearest neighbours.

Spatial map requires a type of ISpatialPosition 

```csharp
public interface ISpatialPosition
{
    float2 Position { get; }
}
```

## Position Builder
If you're just dealing with LocalTransform you can use the built in PositionBuilder efficiently convert LocalTransform into a SpatialPosition array for SpatialMap for you.

## Params

| Param        | Description                                                                                                                                             |
|--------------|---------------------------------------------------------------------------------------------------------------------------------------------------------|
| Size| How big the world is. For example a value of 4096 means 4096x4096 or -2048 to 2048                                                                      |
| QuantizeStep | The size of the chunks to break the world up into. A value of 16 for a size of 4096 means you'll have quantized size of 4096/16 = 256 (256 x 256 cells) |

## Memory Usage
Memory usage of the spatial map is defined by the quantizeSize squared, where quantizeSize is defined as size / quantizeStep;

For reasonable size worlds memory usage remains quite small.
However it's important to note if you have a very large world and a small quantizeStep this will explode.
You can still use this on large worlds, you just need to use a reasonable step.

Example of the explosion and a reasonable step

`size = 8192, quantizeStep = 16. Memory usage is 1MB`

`size = 262144, quantizeStep = 16. Memory usage is 1GB`

`size = 262144, quantizeStep = 128. Memory usage is 4MB`

## Dealing with Burst and Generics
As SpatialMap is generic and contains generic jobs, burst generally does not like compiling this and you usually need to add a bunch of `[RegisterGenericJobType(typeof(T))]`.

However I find this annoying and as a workaround the jobs have been included as optional parameters in the Build method.
```csharp
public JobHandle Build(NativeArray<T> positions, JobHandle dependency, ResizeNativeKeyedMapJob resizeStub = default, QuantizeJob quantizeStub = default)
```
Therefore when using this method these parameters can be ignored. Only the positions and dependency need to be set.

## Example
### Setup

```csharp
public partial struct TestSystem : ISystem
{
    private PositionBuilder positionBuilder;
    private SpatialMap<SpatialPosition> spatialMap;

    public void OnCreate(ref SystemState state)
    {
        var query = SystemAPI.QueryBuilder().WithAll<LocalTransform>().Build();
        this.positionBuilder = new PositionBuilder(ref state, query);

        const int size = 4096;
        const int quantizeStep = 16;

        this.spatialMap = new SpatialMap<SpatialPosition>(quantizeStep, size);
    }

    public void OnDestroy(ref SystemState state)
    {
        this.spatialMap.Dispose();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        state.Dependency = this.positionBuilder.Gather(ref state, state.Dependency, out NativeArray<SpatialPosition> positions);
        state.Dependency = this.spatialMap.Build(positions, state.Dependency);
```

At this point the jobs to build your spatial map have been scheduled and you can pass it to jobs to query it

### Using the SpatialMap

The follow example finds and store all other entities within 10 of each other

```csharp
        // The entities in this will match the indices from the spatial map
        var entities = this.query.ToEntityListAsync(state.WorldUpdateAllocator, state.Dependency, out var dependency);
        state.Dependency = dependency;

        new TestJob
            {
                Entities = entities.AsDeferredJobArray(),
                Positions = positions,
                SpatialMap = this.spatialMap.AsReadOnly(),
            }
            .ScheduleParallel();
    }

    // Find and store all other entities within 10 of each other
    [BurstCompile]
    private partial struct TestJob : IJobEntity
    {
        private const float Radius = 10;

        [ReadOnly]
        public NativeArray<Entity> Entities;

        [ReadOnly]
        public NativeArray<SpatialPosition> Positions;

        [ReadOnly]
        public SpatialMap.ReadOnly SpatialMap;

        private void Execute(Entity entity, in LocalTransform localTransform, DynamicBuffer<Neighbours> neighbours)
        {
            neighbours.Clear();

            // Find the min and max boxes
            var min = this.SpatialMap.Quantized(localTransform.Position.xz - Radius);
            var max = this.SpatialMap.Quantized(localTransform.Position.xz + Radius);

            for (var j = min.y; j <= max.y; j++)
            {
                for (var i = min.x; i <= max.x; i++)
                {
                    var hash = this.SpatialMap.Hash(new int2(i, j));

                    if (!this.SpatialMap.Map.TryGetFirstValue(hash, out int item, out var it))
                    {
                        continue;
                    }

                    do
                    {
                        var otherEntity = this.Entities[item];

                        // Don't add ourselves
                        if (otherEntity.Equals(entity))
                        {
                            continue;
                        }

                        var otherPosition = this.Positions[item].Position;

                        // The spatialmap serves as the broad-phase but most of the time we still need to ensure entities are actually within range
                        if (math.distancesq(localTransform.Position.xz, otherPosition.xz) <= Radius * Radius)
                        {
                            neighbours.Add(new Neighbours { Entity = otherEntity });
                        }
                    }
                    while (this.SpatialMap.Map.TryGetNextValue(out item, ref it));
                }

            }
        }
    }

    public struct Neighbours : IBufferElementData
    {
        public Entity Entity;
    }
}
```
