# Life Cycle

## Summary

The BovineLabs Core Life Cycle system provides a unified framework for managing entity initialization and destruction in Unity Entities.

Key features include:
- Unified initialization component for prefabs and subscene entities
- Automatic destruction propagation through LinkedEntityGroup

## System Architecture

### Components

| Component                  | Purpose                                               |
|----------------------------|-------------------------------------------------------|
| `InitializeEntity`         | Marks prefab entities for initialization              |
| `InitializeSubSceneEntity` | Marks subscene entities for initialization (opt-in)   |
| `DestroyEntity`            | Enableable component that triggers entity destruction |

### System Groups

| System Group            | Purpose                         | Update Order                                                |
|-------------------------|---------------------------------|-------------------------------------------------------------|
| `InitializeSystemGroup` | Processes entity initialization | OrderFirst BeginSimulationSystemGroup - Start of simulation |
| `DestroySystemGroup`    | Processes entity destruction    | Before SceneSystemGroup - Before scene loading              |

### Systems

| System                          | Purpose                                                                                          |
|---------------------------------|--------------------------------------------------------------------------------------------------|
| `InitializeEntitySystem`        | Disables initialization components after processing                                              |
| `DestroyOnDestroySystem`        | Propagates destruction through LinkedEntityGroup                                                 |
| `DestroyOnSubSceneUnloadSystem` | Allows the destroy pipeline to execute on entities that are about to be unloaded from a subscene |
| `DestroyEntitySystem`           | Performs actual entity destruction                                                               |

### Command Buffer Systems

| System                                   | Purpose                      |
|------------------------------------------|------------------------------|
| `EndInitializeEntityCommandBufferSystem` | ECB for initialization phase |
| `DestroyEntityCommandBufferSystem`       | ECB for destruction phase    |

## Basic Usage

### Setup

Add a `LifeCycleAuthoring` component to any GameObject that needs lifecycle management. 
Optionally you can add it for AdditionalEntities using `LifeCycleAuthoring.AddComponents(IBaker, Entity, bool isPrefab)`.

This automatically adds:
- `InitializeEntity` for prefabs
- `InitializeSubSceneEntity` for entities in subscenes
- `DestroyEntity`

### Initialize Entity

Create systems that run on entity initialization:

```csharp
[UpdateInGroup(typeof(InitializeSystemGroup))]
public partial struct InitializePlayerSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Schedule job to initialize entities
        new InitializeJob().ScheduleParallel();
    }
    
    [BurstCompile]
    [WithAll(typeof(InitializeEntity))]
    private partial struct InitializeJob : IJobEntity
    {
        private void Execute(Entity entity, ref Player player)
        {
            // Initialize player data
            player.Health = 100;
            player.Score = 0;
        }
    }
}
```

You can customize which entities to initialize by changing the query attributes:

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
[BurstCompile]
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

#### Processing Before Destruction

To perform actions on entities before they're destroyed, create systems in the `DestroySystemGroup`:

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
    [WithAll(typeof(Monster))]
    [WithAll(typeof(DestroyEntity))] // Only process entities marked for destruction
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