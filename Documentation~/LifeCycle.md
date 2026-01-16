# Life Cycle

## Summary

The Life Cycle system provides a unified framework for managing entity initialization and destruction in Unity Entities.

**Key Features:**
- Unified initialization components for prefabs and subscene entities
- Automatic destruction propagation through LinkedEntityGroup

## Core Components

**Components:**
- `InitializeEntity`: Marks prefab entities for initialization
- `InitializeSubSceneEntity`: Marks subscene entities for initialization (opt-in)
- `DestroyEntity`: Enableable component that triggers entity destruction

**System Groups:**
- `InitializeSystemGroup`: Processes entity initialization. Updates before DestroySystemGroup to allow access to data from destroyed entities.
- `DestroySystemGroup`: Processes entity destruction. Runs before SceneSystemGroup to properly cleanup entities in closing subscenes.

**Core Systems:**
- `SceneInitializeSystem`: Runs first in BeginSimulationSystemGroup and calls InitializeSystemGroup to process subscene/ghost entities.

**Command Buffer Systems:**
- `InstantiateCommandBufferSystem`: Handles entity instantiation operations for the initialization lifecycle phase
- `EndInitializeEntityCommandBufferSystem`: ECB for initialization phase
- `DestroyEntityCommandBufferSystem`: ECB for destruction phase

**Destruction Systems:**
- `DestroyEntitySystem`: Core system that destroys entities marked with DestroyEntity
- `DestroyOnDestroySystem`: Propagates destruction through LinkedEntityGroup hierarchies
- `DestroyOnSubSceneUnloadSystem`: Automatically destroys entities when subscenes are unloaded

**Utilities:**
- `DestroyTimer<T>`: Generic utility for timer-based entity destruction

## Setup

Add `LifeCycleAuthoring` to GameObjects needing lifecycle management:
- Prefabs get `InitializeEntity` + `DestroyEntity`
- Subscene entities get `InitializeSubSceneEntity` + `DestroyEntity`

For AdditionalEntities, use `LifeCycleAuthoring.AddComponents(IBaker, Entity, bool isPrefab)`.

## Usage

### Initialize Entity

Create systems that run on entity initialization:

```csharp
[UpdateInGroup(typeof(InitializeSystemGroup))]
public partial struct InitializePlayerSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        new InitializeJob().ScheduleParallel();
    }
    
    [BurstCompile]
    [WithAll(typeof(InitializeEntity))]
    private partial struct InitializeJob : IJobEntity
    {
        private void Execute(Entity entity, ref Player player)
        {
            player.Health = 100;
            player.Score = 0;
        }
    }
}
```

**Query Options:**
```csharp
// Initialize only entities instantiated from prefabs
[WithAll(typeof(InitializeEntity))]

// Initialize only entities created in subscenes
[WithAll(typeof(InitializeSubSceneEntity))]

// Initialize both prefab and subscene entities
[WithAny(typeof(InitializeEntity), typeof(InitializeSubSceneEntity))]
```

### Destroy Entity

Trigger entity destruction by enabling the `DestroyEntity` component:

```csharp
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct HealthDestroySystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        new DestroyJob().ScheduleParallel();
    }
    
    [BurstCompile]
    private partial struct DestroyJob : IJobEntity
    {
        private void Execute(in Health health, EnabledRefRW<DestroyEntity> destroy)
        {
            if (health.Value <= 0)
            {
                destroy.ValueRW = true;
            }
        }
    }
}
```

### Processing Before Destruction

Perform actions on entities before they're destroyed using `DestroySystemGroup`:

```csharp
[UpdateInGroup(typeof(DestroySystemGroup))]
public partial struct TrackDestroyedMonsterSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        new TrackJob { Tracking = SystemAPI.GetSingleton<DestroyedMonsterList>().Map }.Schedule();
    }
    
    [BurstCompile] 
    [WithAll(typeof(Monster), typeof(DestroyEntity))]
    private partial struct TrackJob : IJobEntity
    {
        public NativeHashMap<ObjectId, int> Tracking;
        
        private void Execute(in ObjectId id)
        {
            Tracking.GetOrAddRef(id)++;
        }
    }
}
```

### Destruction Propagation

When an entity with `DestroyEntity` enabled has a `LinkedEntityGroup`, destruction automatically propagates to all child entities:

- `DestroyOnDestroySystem` runs first and recursively enables `DestroyEntity` on all entities in the hierarchy
- Child entities are removed from the LinkedEntityGroup so they can be processed independently
- Already destroyed entities are safely handled and removed from the group
- This ensures proper cleanup of complex entity hierarchies like vehicles with multiple parts

**Automatic Subscene Cleanup:**

When subscenes are unloaded, `DestroyOnSubSceneUnloadSystem` automatically enables `DestroyEntity` on all entities belonging to that subscene, ensuring no orphaned entities remain in memory.

### Timer-Based Destruction

Use `DestroyTimer<T>` for automatic entity destruction after a specified time:

```csharp
// Define a timer component (must be same size as float)
public struct ExplosionTimer : IComponentData
{
    public float Value;
}

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct ExplosionTimerSystem : ISystem
{
    private DestroyTimer<ExplosionTimer> destroyTimer;

    public void OnCreate(ref SystemState state)
    {
        destroyTimer.OnCreate(ref state);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        destroyTimer.OnUpdate(ref state);
    }
}
```

The timer component will be decremented each frame, and when it reaches zero, the `DestroyEntity` component will be automatically enabled.
