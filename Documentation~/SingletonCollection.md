# Singleton Collections
## Summary
Easily set up a Many-To-One container singleton with minimal boilerplate and syncless job support.

Think of it like EntityCommandBuffer.Singleton, but with the added capability of performing reading in a job.

It currently supports the following Unity Collection containers

- NativeArray
- NativeList
- NativeQueue
- NativeHashMap
- NativeParallelHashMap
- NativeParallelMultiHashMap

as well as the following Core containers
- NativeThreadStream
- NativeMultiHashMap

## Example
### Reading
This is the system that owns the singleton and processes the collection of containers that are passed to it each frame.
```csharp
public partial struct MySystem : ISystem
{
    private SingletonCollectionUtil<Singleton, NativeList<int>> singletonCollectionUtil;

    // Note this can't be burst compiled due to RewindAllocator creation
    public void OnCreate(ref SystemState state)
    {
        this.singletonCollectionUtil = new SingletonCollectionUtil<Singleton, NativeList<int>>(ref state);
    }

    // Note this can't be burst compiled due to RewindAllocator disposable
    public void OnDestroy(ref SystemState state)
    {
        this.singletonCollectionUtil.Dispose();
    }

    [BurstCompile]
    public unsafe void OnUpdate(ref SystemState state)
    {
        var lists = this.singletonCollectionUtil.Containers;

        for (var i = 0; i < lists.Length; i++)
        {
            state.Dependency = new Job { List = lists.Ptr[i] }.Schedule(state.Dependency);
        }

        this.singletonCollectionUtil.ClearRewind();
    }

    // Define the singleton
    public struct Singleton : ISingletonCollection<NativeList<int>>
    {
        /// <inheritdoc/>
        unsafe UnsafeList<NativeList<int>>* ISingletonCollection<NativeList<int>>.Collections { get; set; }

        /// <inheritdoc/>
        Allocator ISingletonCollection<NativeList<int>>.Allocator { get; set; }
    }

    [BurstCompile]
    private struct Job : IJob
    {
        public NativeList<int> List;

        public void Execute()
        {
            // Do something
        }
    }
}
```

### Writing
Creating a container to write to is as simple as
```csharp
NativeList<int> list = SystemAPI.GetSingleton<MySystem.Singleton>().CreateList(capacity);
```

The options for creating containers are as follows:
#### NativeArray
`NativeArray<T> CreateArray<T>(int length, NativeArrayOptions options = NativeArrayOptions.ClearMemory)`
#### NativeList
`NativeList<T> CreateList<T>(int capacity)`
#### NativeQueue
`NativeQueue<T> CreateQueue<T>()`
#### NativeHashMap
`NativeHashMap<TKey, TValue> CreateHashMap<TKey, TValue>(int capacity)`
#### NativeMultiHashMap
`NativeMultiHashMap<TKey, TValue> CreateMultiHashMap<TKey, TValue>(int capacity)`
#### NativeParallelMultiHashMap
`NativeParallelHashMap<TKey, TValue> CreateParallelHashMap<TKey, TValue>(int capacity)`
#### NativeParallelMultiHashMap
`NativeParallelMultiHashMap<TKey, TValue> CreateParallelMultiHashMap<TKey, TValue>(int capacity)`
#### NativeThreadStream
`NativeThreadStream.Writer CreateThreadStream();`
