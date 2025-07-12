# Life Cycle

## Summary

The Life Cycle system provides a unified framework for managing entity initialization and destruction in Unity Entities.

**Key Features:**
- Unified initialization component for prefabs and subscene entities
- Automatic destruction propagation through LinkedEntityGroup

## Core Components

**Components:**
- `InitializeEntity`: Marks prefab entities for initialization
- `InitializeSubSceneEntity`: Marks subscene entities for initialization (opt-in)
- `DestroyEntity`: Enableable component that triggers entity destruction

**System Groups:**
- `InitializeSystemGroup`: Processes entity initialization (start of simulation)
- `DestroySystemGroup`: Processes entity destruction (before scene loading)

**Command Buffer Systems:**
- `EndInitializeEntityCommandBufferSystem`: ECB for initialization phase
- `DestroyEntityCommandBufferSystem`: ECB for destruction phase

## Setup

Add a `LifeCycleAuthoring` component to any GameObject that needs lifecycle management. This automatically adds:
- `InitializeEntity` for prefabs
- `InitializeSubSceneEntity` for entities in subscenes
- `DestroyEntity`

For AdditionalEntities, use `LifeCycleAuthoring.AddComponents(IBaker, Entity, bool isPrefab)`.

## Usage

### Initialize Entity

Create systems that run on entity initialization:

```csharp
[UpdateInGroup(typeof(InitializeSystemGroup))]
public partial struct InitializePlayerSystem : ISystem
{
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