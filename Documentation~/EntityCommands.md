# IEntityCommands

## Summary

IEntityCommands provides a unified interface for entity manipulation across different contexts in Unity DOTS. Write generic methods that work with EntityManager, EntityCommandBuffer, EntityCommandBuffer.ParallelWriter, and IBaker without code duplication.

**Key Features:**
- Single interface for all entity manipulation contexts
- Consistent API across immediate and deferred execution modes
- Simplified code reuse for entity operations

**Available Implementations:**
- **EntityManagerCommands**: Direct immediate execution
- **CommandBufferCommands**: Deferred execution via EntityCommandBuffer
- **CommandBufferParallelCommands**: Parallel deferred execution
- **BakerCommands**: Baking-time entity manipulation

## Common Methods

```cs
// Component operations
void AddComponent<T>(T component) where T : unmanaged, IComponentData;
void SetComponent<T>(T component) where T : unmanaged, IComponentData;
void RemoveComponent<T>() where T : unmanaged;

// Buffer operations
DynamicBuffer<T> AddBuffer<T>() where T : unmanaged, IBufferElementData;
DynamicBuffer<T> GetBuffer<T>() where T : unmanaged, IBufferElementData;

// Entity operations
Entity CreateEntity();
void DestroyEntity();
Entity Instantiate(Entity prefab);

// Naming operations
void SetName(FixedString64Bytes name);
void SetName(Entity entity, FixedString64Bytes name);
```

## Usage

### Generic Entity Setup
Create reusable methods that work with any IEntityCommands implementation:

```cs
public static void SetupMovementEntity<T>(ref T commands, float3 position, float3 velocity)
    where T : unmanaged, IEntityCommands
{
    commands.AddComponent(LocalTransform.FromPosition(position));
    commands.AddComponent(new PhysicsVelocity { Linear = velocity });
    commands.AddComponent<MovementTag>();
}
```

### System Usage
```cs
[BurstCompile]
public partial struct MovementSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
        
        foreach (var (spawn, entity) in SystemAPI.Query<RefRO<SpawnRequest>>().WithEntityAccess())
        {
            var newEntity = ecb.CreateEntity();
            var commands = new CommandBufferCommands(ecb, newEntity);
            
            SetupMovementEntity(ref commands, spawn.ValueRO.Position, spawn.ValueRO.Velocity);
            ecb.DestroyEntity(entity);
        }
        
        ecb.Playback(state.EntityManager);
    }
}
```

### Job Usage
```cs
[BurstCompile]
public struct ProcessEntitiesJob : IJobChunk
{
    public EntityCommandBuffer.ParallelWriter CommandBuffer;
    
    public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
    {
        // Use parallel command buffer
        var commands = new CommandBufferParallelCommands(CommandBuffer, entity, unfilteredChunkIndex);
        SetupMovementEntity(ref commands, data.Position, data.Velocity);
    }
}
```

### Baking Usage
```cs
public class Baker : Baker<MovementAuthoring>
{
    public override void Bake(MovementAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        var commands = new BakerCommands(this, entity);
        
        SetupMovementEntity(ref commands, float3.zero, authoring.InitialVelocity);
    }
}
```