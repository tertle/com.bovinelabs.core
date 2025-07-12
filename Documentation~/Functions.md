# Functions

## Summary

Functions provide an extensible way to add support for extending jobs to other developers or modders. Pass function pointers to jobs that forward to methods on data-containing structs.

**Key Features:**
- Extensible job system for modding support
- Function pointers with data forwarding
- Automatic discovery of implementations
- Manual registration of functions

## Core Components

- **IFunction<T>**: Interface for implementing extensible functions
- **FunctionsBuilder<T, TO>**: Builder for creating Functions collections
- **Functions<T, TO>**: Collection of functions that can execute in jobs

## Key Methods

```csharp
// Builder methods
new FunctionsBuilder<T, TO>(Allocator.Temp)
    .ReflectAll(ref state)  // Find all IFunction<T> implementations
    .Add<TF>(ref state)     // Manually add function
    .Build();               // Create Functions<T, TO>

// Functions methods
functions.Update(ref state);           // Update all functions
functions.Execute(index, ref data);    // Execute specific function
functions.OnDestroy(ref state);        // Cleanup
```

## Usage

### System Setup
```csharp
public partial struct ExampleSystem : ISystem
{
    private Functions<InstantiateData, Entity> instantiateFunctions;

    public void OnCreate(ref SystemState state)
    {
        instantiateFunctions = new FunctionsBuilder<InstantiateData, Entity>(Allocator.Temp)
            .ReflectAll(ref state) // Find all IFunction<InstantiateData>
            .Build();
    }

    public void OnDestroy(ref SystemState state)
    {
        instantiateFunctions.OnDestroy(ref state);
    }

    public void OnUpdate(ref SystemState state)
    {
        instantiateFunctions.Update(ref state);
        
        new SpawnJob { InstantiateFunctions = instantiateFunctions }.Schedule();
    }

    public struct InstantiateData
    {
        public Entity Prefab;
        public EntityCommandBuffer EntityCommandBuffer;
        public float3 Position;
    }
}
```

### Job Usage
```csharp
[BurstCompile]
private partial struct SpawnJob : IJobEntity
{
    public Functions<InstantiateData, Entity> InstantiateFunctions;

    private void Execute(Entity entity, in Spawner spawner, in LocalTransform localTransform)
    {
        var data = new InstantiateData
        {
            Prefab = spawner.Prefab,
            Position = localTransform.Position,
            EntityCommandBuffer = EntityCommandBuffer,
        };

        for (var i = 0; i < InstantiateFunctions.Length; i++)
        {
            if (InstantiateFunctions.Execute(i, ref data) != Entity.Null)
            {
                return; // Found a match
            }
        }
    }
}
```

### Function Implementation
```csharp
[BurstCompile]
public unsafe struct InstantiateTestUnit : IFunction<ExampleSystem.InstantiateData>
{
    private UnsafeComponentLookup<TestUnit> testUnit;

    public UpdateFunction? UpdateFunction => Update;
    public DestroyFunction? DestroyFunction => null;
    public ExecuteFunction ExecuteFunction => Execute;

    public void OnCreate(ref SystemState state)
    {
        testUnit = state.GetUnsafeComponentLookup<TestUnit>(true);
    }

    private Entity Execute(ref ExampleSystem.InstantiateData data)
    {
        if (!testUnit.HasComponent(data.Prefab))
            return Entity.Null;

        var instance = data.EntityCommandBuffer.Instantiate(data.Prefab);
        var position = LocalTransform.FromPosition(data.Position + new float3(0, 1, 0));
        data.EntityCommandBuffer.SetComponent(instance, position);
        return instance;
    }

    // Required forwarding functions
    [BurstCompile]
    [AOT.MonoPInvokeCallback(typeof(UpdateFunction))]
    private static void Update(void* target, ref SystemState state)
    {
        ((InstantiateTestUnit*)target)->testUnit.Update(ref state);
    }

    [BurstCompile]
    [AOT.MonoPInvokeCallback(typeof(ExecuteFunction))]
    private static void Execute(void* target, void* data, void* result)
    {
        *(Entity*)result = ((InstantiateTestUnit*)target)->Execute(ref UnsafeUtility.AsRef<ExampleSystem.InstantiateData>(data));
    }
}
```
