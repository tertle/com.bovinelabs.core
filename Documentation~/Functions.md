# Functions
## Summary
Functions provide an easy way to add support for extending jobs to other developers or modders. 
It allows function pointers to be passed to a job that forward to a method on a struct that contains data.

### IFunction

```csharp
public unsafe delegate void DestroyFunction(void* target, ref SystemState state);
public unsafe delegate void UpdateFunction(void* target, ref SystemState state);
public unsafe delegate void ExecuteFunction(void* target, void* data, void* result);

/// <summary> An implementation of a forwarding function pointer for extending jobs to other developers or modders. </summary>
/// <typeparam name="T"> Is the void* data that will be passed to the ExecuteFunction. Also serves as a grouping mechanism for ReflectAll. </typeparam>
public interface IFunction<T>
    where T : unmanaged
{
    /// <summary>
    /// Gets the OnDestroy forwarding function which must be a static forwarding function however it is never burst compiled.
    /// Should be called from a Systems OnDestroy to cleanup any allocated memory.
    /// Optional, return null if not required.
    /// </summary>
    DestroyFunction? DestroyFunction { get; }

    /// <summary>
    /// Gets the OnUpdate forwarding function which must be a static forwarding function and burst compilable.
    /// Should be called from a systems OnUpdate and allows you to update Lookups etc if required.
    /// As safety will not work, you must use the provided UnsafeComponentLookup and UnsafeBufferLookup.
    /// Optional, return null if not required.
    /// </summary>
    UpdateFunction? UpdateFunction { get; }

    /// <summary>
    /// Gets the OnUpdate forwarding function which must be a static forwarding function and burst compilable.
    /// The logic that will execute inside the job when requested.
    /// </summary>
    ExecuteFunction ExecuteFunction { get; }

    /// <summary> Called directly from the builder to setup the struct if required. </summary>
    /// <param name="state"> The system state. </param>
    void OnCreate(ref SystemState state);
}
```

### FunctionsBuilder
```csharp
/// <summary> The builder for creating <see cref="Functions{T}"/>. </summary>
/// <typeparam name="T"> Is the void* data that will be passed to the ExecuteFunction. Also serves as a grouping mechanism for ReflectAll. </typeparam>
/// <typeparam name="TO"> Is the type of result that is expected from the ExecuteFunction. </typeparam>
public unsafe struct FunctionsBuilder<T, TO> : IDisposable
    where T : unmanaged
    where TO : unmanaged
{
    /// <summary> Initializes a new instance of the <see cref="FunctionsBuilder{T}"/> struct. </summary>
    /// <param name="allocator"> The allocator to use for the builder. This should nearly always be <see cref="Allocator.Temp"/>. </param>    
    public FunctionsBuilder(Allocator allocator);
    
    public void Dispose();

    /// <summary> Find all implementations of <see cref="IFunction{T}"/>. </summary>
    public FunctionsBuilder<T. TO> ReflectAll(ref SystemState state);

    /// <summary> Manually add an instance of <see cref="IFunction{T}"/>. </summary>
    public FunctionsBuilder<T, TO> Add<TF>(ref SystemState state, TF function);

    /// <summary> Manually create an instance of <see cref="IFunction{T}"/>. </summary>
    public FunctionsBuilder<T, TO> Add<TF>(ref SystemState state);

    /// <summary> Builds the <see cref="Functions{T, TO}"/> to use with all the found <see cref="IFunction{T}"/>. </summary>
    public Functions<T, TO> Build();
}
```

### Functions<T>

```csharp
/// <summary> The collection of forwarding functions that can be executed in a burst job. </summary>
/// <typeparam name="T"> Is the void* data that will be passed to the ExecuteFunction. Also serves as a grouping mechanism for ReflectAll. </typeparam>
/// <typeparam name="TO"> Is the type of result that is expected from the ExecuteFunction. </typeparam>
public unsafe struct Functions<T, TO>
    where T : unmanaged
    where TO : unmanaged
{
  /// <summary> Gets the number of functions for iterating. </summary>
  public int Length { get; }

  /// <summary> Call this in OnDestroy on the system to dispose memory. It also calls OnDestroy on all IFunction. </summary>
  public void OnDestroy(ref SystemState state);

  /// <summary> Call in OnUpdate to call OnUpdate on all IFunction. </summary>
  public void Update(ref SystemState state);

  /// <summary> Call to execute a specific function. </summary>
  /// <param name="index"> The index of function to call. Should be positive and less than Length. </param>
  /// <param name="data"> The data to pass to the function. </param>
  /// <returns> A user defined value. Can use 0 as false for example. </returns>
  public TO Execute(int index, ref T data);
}
```

## Example

### ISystem
```csharp
  public partial struct ExampleSystem : ISystem
  {
      private Functions<InstantiateData, Entity> instantiateFunctions;

      public void OnCreate(ref SystemState state)
      {
          // Can't be burst compiled when using ReflectAll
          this.instantiateFunctions = new FunctionsBuilder<InstantiateData, Entity>(Allocator.Temp)
              .ReflectAll(ref state) // Will find all IFunction<InstantiateData>
              .Build();
      }

      public void OnDestroy(ref SystemState state)
      {
          this.instantiateFunctions.OnDestroy(ref state);
      }

      [BurstCompile]
      public void OnUpdate(ref SystemState state)
      {
          this.instantiateFunctions.Update(ref state);

          new SpawnJob
              {
                  InstantiateFunctions = this.instantiateFunctions,
                  EntityCommandBuffer = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged),
              }
              .Schedule();
      }

      // Define our data
      public struct InstantiateData
      {
          public Entity Prefab;
          public EntityCommandBuffer EntityCommandBuffer;
          public float3 Position;
      }

      [BurstCompile]
      private partial struct SpawnJob : IJobEntity
      {
          public Functions<InstantiateData, Entity> InstantiateFunctions;

          public EntityCommandBuffer EntityCommandBuffer;

          private void Execute(Entity entity, in Spawner spawner, in LocalTransform localTransform)
          {
              this.EntityCommandBuffer.DestroyEntity(entity);

              var data = new InstantiateData
              {
                  Prefab = spawner.Prefab,
                  Position = localTransform.Position,
                  EntityCommandBuffer = this.EntityCommandBuffer,
              };

              for (var i = 0; i < this.InstantiateFunctions.Length; i++)
              {
                  if (this.InstantiateFunctions.Execute(i, ref data) != Entity.Null)
                  {
                      // We found a match, stop iterating
                      return;
                  }
              }
          }
      }
  }
```

### IFunction Implementation
```csharp
[BurstCompile]
public unsafe struct InstantiateTestUnit : IFunction<ExampleSystem.JobData>
{
    private UnsafeComponentLookup<TestUnit> testUnit;

    public UpdateFunction? UpdateFunction => Update;

    public DestroyFunction? DestroyFunction => null;

    public ExecuteFunction ExecuteFunction => Execute;

    public void OnCreate(ref SystemState state)
    {
        this.testUnit = state.GetUnsafeComponentLookup<TestUnit>(true);
    }

    private void OnUpdate(ref SystemState state)
    {
        this.testUnit.Update(ref state);
    }

    private Entity Execute(ref ExampleSystem.JobData data)
    {
        if (!this.testUnit.HasComponent(data.Prefab))
        {
            return Entity.Null;
        }

        var instance = data.EntityCommandBuffer.Instantiate(data.Prefab);
        var positionWithOffset = LocalTransform.FromPosition(data.Position + new float3(0, 1, 0));
        data.EntityCommandBuffer.SetComponent(instance, positionWithOffset);

        return instance;
    }

    // Forwarding functions
    [BurstCompile]
    [AOT.MonoPInvokeCallback(typeof(UpdateFunction))]
    private static void Update(void* target, ref SystemState state)
    {
        ((InstantiateTestUnit*)target)->OnUpdate(ref state);
    }

    [BurstCompile]
    [AOT.MonoPInvokeCallback(typeof(ExecuteFunction))]
    private static void Execute(void* target, void* data, void* result)
    {
        *(Entity*)result = ((InstantiateTestUnit*)target)->Execute(ref UnsafeUtility.AsRef<ExampleSystem.JobData>(data));
    }
}
```
