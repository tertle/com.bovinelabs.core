# IEntityCommands

`IEntityCommands` lets one entity-construction method work with a baker, an immediate `EntityManager`, a main-thread `EntityCommandBuffer`, or a parallel command-buffer writer.

```csharp
using BovineLabs.Core.EntityCommands;

// BakerCommands is in the authoring assembly.
using BovineLabs.Core.Authoring.EntityCommands;
```

## Choose an implementation

| Context | Wrapper | Construction | Mutation timing |
|---|---|---|---|
| Baker | `BakerCommands` | `new BakerCommands(baker, entity)` | During baking |
| Tests or editor tooling | `EntityManagerCommands` | `new EntityManagerCommands(entityManager, entity)` | Immediate |
| Main-thread deferred work | `CommandBufferCommands` | `new CommandBufferCommands(commandBuffer, entity)` | At command-buffer playback |
| Parallel job | `CommandBufferParallelCommands` | `new CommandBufferParallelCommands(writer, sortKey, entity)` | At command-buffer playback |

The parallel constructor order is `writer, sortKey, localEntity`. Pass the job's stable chunk or entity sort key as the second argument.

`BakerCommands` implements the common interface, but `Instantiate`, `SetName`, and `AppendToBuffer` throw because those operations are not supported by a baker. Use `CreateEntity`, baker naming APIs, and the buffer returned by `AddBuffer` or `SetBuffer` instead.

## Reusable builder pattern

Constrain reusable builders to `struct, IEntityCommands`. Do not use `unmanaged`: `BakerCommands` stores an `IBaker` reference and is therefore not unmanaged.

```csharp
public struct MovementData : IComponentData
{
    public float3 Position;
    public float3 Velocity;
}

public struct MovementTag : IComponentData
{
}

public static class MovementEntityBuilder
{
    public static void Configure<T>(ref T commands, float3 position, float3 velocity)
        where T : struct, IEntityCommands
    {
        commands.AddComponent(new MovementData
        {
            Position = position,
            Velocity = velocity,
        });

        commands.AddComponent<MovementTag>();
    }
}
```

Keep queries, spawn policy, system ordering, and other context-specific work outside the generic builder. The builder should describe the entity shape.

## Entity selection

Every wrapper has a mutable `Entity` property. Operations without an explicit entity target that property.

- The constructor's optional `localEntity` initializes `Entity`.
- `CreateEntity()` replaces `Entity` with the new entity.
- `Instantiate(prefab)` replaces `Entity` with the new instance.
- Use explicit-entity overloads when a method touches more than one entity.

```csharp
var commands = new EntityManagerCommands(entityManager);
var entity = commands.CreateEntity();

MovementEntityBuilder.Configure(ref commands, float3.zero, new float3(0, 0, 5));

// entity == commands.Entity
```

## Supported operations

| Area | Operations |
|---|---|
| Entity | `Entity`, `CreateEntity()`, `Instantiate(prefab)` |
| Components | `AddComponent<T>()`, `AddComponent<T>(in value)`, `AddComponent(in ComponentTypeSet)`, `SetComponent<T>(in value)`, plus explicit-entity overloads |
| Buffers | `AddBuffer<T>()`, `SetBuffer<T>()`, `AppendToBuffer<T>(in value)`, plus explicit-entity overloads |
| Enableable components | `SetComponentEnabled<T>(bool)` and its explicit-entity overload |
| Shared components | `AddSharedComponent<T>(entity, in value)`, `SetSharedComponent<T>(entity, in value)` |
| Blob assets | `AddBlobAsset<T>(ref blob, out hash)` |
| Debug names | `SetName(name)` and `SetName(entity, name)` |

`IEntityCommands` does not expose component removal, entity destruction, or arbitrary buffer lookup. Perform those context-specific operations through the owning baker, entity manager, or command buffer.

Use `AddBuffer` when the buffer is absent. Use `SetBuffer` when replacing and clearing an existing buffer. Filling the returned buffer works in every wrapper, including baking:

```csharp
public struct Waypoint : IBufferElementData
{
    public float3 Position;
}

public static void AddWaypoints<T>(ref T commands, NativeArray<float3> positions)
    where T : struct, IEntityCommands
{
    var waypoints = commands.AddBuffer<Waypoint>();

    foreach (var position in positions)
    {
        waypoints.Add(new Waypoint { Position = position });
    }
}
```

## Baking

```csharp
public sealed class MovementAuthoring : MonoBehaviour
{
    public float3 Position;
    public float3 InitialVelocity;
}

public sealed class MovementBaker : Baker<MovementAuthoring>
{
    public override void Bake(MovementAuthoring authoring)
    {
        var entity = this.GetEntity(TransformUsageFlags.Dynamic);
        var commands = new BakerCommands(this, entity);

        MovementEntityBuilder.Configure(ref commands, authoring.Position, authoring.InitialVelocity);
    }
}
```

The builder must not call the three baker-unsupported operations listed above.

## Parallel command-buffer job

```csharp
public struct SpawnRequest : IComponentData
{
    public float3 Position;
    public float3 Velocity;
}

[BurstCompile]
public partial struct MovementSpawnSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var commandBuffer = SystemAPI
            .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged)
            .AsParallelWriter();

        state.Dependency = new SpawnJob
        {
            CommandBuffer = commandBuffer,
        }.ScheduleParallel(state.Dependency);
    }

    [BurstCompile]
    private partial struct SpawnJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter CommandBuffer;

        private void Execute([ChunkIndexInQuery] int sortKey, Entity requestEntity, in SpawnRequest request)
        {
            var commands = new CommandBufferParallelCommands(this.CommandBuffer, sortKey);
            commands.CreateEntity();

            MovementEntityBuilder.Configure(ref commands, request.Position, request.Velocity);

            // Destruction is not part of IEntityCommands, so keep it in the orchestration layer.
            this.CommandBuffer.DestroyEntity(sortKey, requestEntity);
        }
    }
}
```

Do not play back a command buffer while jobs that write to it are still running. When a system-provided command-buffer singleton owns playback, assign the scheduled handle to `state.Dependency`.

## Blob assets

`BakerCommands.AddBlobAsset` delegates to the baker and participates in normal baking deduplication. The other wrappers only deduplicate when constructed with a valid `BlobAssetStore`; without one they leave the returned hash at its default value.

## Common failures

| Symptom | Check |
|---|---|
| Generic builder rejects `BakerCommands` | Use `where T : struct, IEntityCommands`, not `unmanaged` |
| Parallel constructor does not compile | Pass `sortKey` before `localEntity` |
| Components land on the wrong entity | Check whether `CreateEntity` or `Instantiate` changed `commands.Entity` |
| Baker throws `NotImplementedException` | Remove `Instantiate`, `SetName`, or `AppendToBuffer` from the shared builder |
| Duplicate buffer/component during baking | Match `Add*` versus `Set*` to whether the data already exists |
| Blob deduplication is missing outside baking | Supply a `BlobAssetStore` to the wrapper |

See [Jobs](Jobs.md) for scheduling the parallel work and [DynamicHashMap](DynamicHashMap.md) for generated dynamic-buffer containers.
