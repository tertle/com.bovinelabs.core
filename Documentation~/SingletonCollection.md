# Singleton collections

Singleton collections let several systems publish temporary native containers to one owning system. The owner sees every container created for the current generation, schedules consumers, and then advances a double-rewindable allocator without completing the current generation immediately.

This is similar in shape to an `EntityCommandBuffer.Singleton`, but the payload is an ordinary native container that jobs can read or write directly.

```csharp
using BovineLabs.Core.SingletonCollection;
```

## When to use it

Use this pattern when:

- multiple ordered systems need to publish variable-sized work;
- the published data is temporary and can be discarded after the owner processes it;
- producers need native containers that can be passed to jobs; and
- one system can own allocator rotation and lifetime.

Do not use it for persistent state, data that must survive allocator generations, or unordered concurrent container creation. The `Create*` extensions append to an unsafe list and must be called from system/main-thread code, not concurrently from jobs. After creating a container, its writer can be passed to jobs where the container supports that access pattern.

## Supported creation helpers

| Stored container | Singleton extension | Result |
|---|---|---|
| `NativeArray<T>` | `CreateArray<TSingleton, T>(length, options)` | Array |
| `NativeList<T>` | `CreateList<TSingleton, T>(capacity)` | List |
| `NativeQueue<T>` | `CreateQueue<TSingleton, T>()` | Queue |
| `NativeHashMap<TKey, TValue>` | `CreateHashMap<TSingleton, TKey, TValue>(capacity)` | Hash map |
| `NativeMultiHashMap<TKey, TValue>` | `CreateMultiHashMap<TSingleton, TKey, TValue>(capacity)` | Core multi-hash map |
| `NativeParallelHashMap<TKey, TValue>` | `CreateParallelHashMap<TSingleton, TKey, TValue>(capacity)` | Parallel hash map |
| `NativeParallelMultiHashMap<TKey, TValue>` | `CreateParallelMultiHashMap<TSingleton, TKey, TValue>(capacity)` | Parallel multi-hash map |
| `NativeThreadStream` | `CreateThreadStream<TSingleton>()` | `NativeThreadStream.Writer` |

The singleton type chooses one stored container type through `ISingletonCollection<TContainer>`.

## Define the singleton component

```csharp
public struct GameEvent
{
    public int Value;
}

public unsafe struct EventSingleton : ISingletonCollection<NativeList<GameEvent>>
{
    UnsafeList<NativeList<GameEvent>>* ISingletonCollection<NativeList<GameEvent>>.Collections { get; set; }

    Allocator ISingletonCollection<NativeList<GameEvent>>.Allocator { get; set; }
}
```

`SingletonCollectionUtil<TSingleton, TContainer>` initializes these fields and adds the singleton component to the owning system entity. Do not create or replace the component yourself.

## Own and consume the collection

`OnCreate` constructs the utility. At the end of each update, pass `ClearRewind` a handle that covers every producer and consumer still using the current generation.

```csharp
public partial struct EventSystem : ISystem
{
    private SingletonCollectionUtil<EventSingleton, NativeList<GameEvent>> events;

    public void OnCreate(ref SystemState state)
    {
        this.events = new SingletonCollectionUtil<EventSingleton, NativeList<GameEvent>>(ref state);
    }

    public void OnDestroy(ref SystemState state)
    {
        state.Dependency.Complete();
        this.events.Dispose();
    }

    [BurstCompile]
    public unsafe void OnUpdate(ref SystemState state)
    {
        var containers = this.events.Containers;
        var handle = state.Dependency;

        for (var i = 0; i < containers.Length; i++)
        {
            handle = new ProcessEventsJob
            {
                Events = containers.Ptr[i],
            }.Schedule(handle);
        }

        state.Dependency = handle;
        this.events.ClearRewind(state.Dependency);
    }

    [BurstCompile]
    private struct ProcessEventsJob : IJob
    {
        [ReadOnly]
        public NativeList<GameEvent> Events;

        public void Execute()
        {
            for (var i = 0; i < this.Events.Length; i++)
            {
                // Process this.Events[i].
            }
        }
    }
}
```

`ClearRewind(JobHandle)` clears the published-container list, completes the handle retained for the allocator generation that is about to be reused, records the supplied current handle, and rotates the allocator. It does not complete the supplied current handle immediately.

Complete all outstanding work before `Dispose()`. `Dispose()` releases the persistent utility state but does not complete retained jobs for you.

## Publish from another system

Order producer systems before the owner and keep their job dependencies connected. The singleton collection does not establish system ordering automatically. Access the singleton through ECS APIs and schedule from `state.Dependency`; bypassing that component access or scheduling from a default handle can hide producer work from the owner's dependency chain.

```csharp
[BurstCompile]
public partial struct GameplayEventSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var singleton = SystemAPI.GetSingleton<EventSingleton>();
        var events = singleton.CreateList<EventSingleton, GameEvent>(capacity: 32);

        state.Dependency = new ProduceEventsJob
        {
            Events = events,
        }.Schedule(state.Dependency);
    }

    [BurstCompile]
    private struct ProduceEventsJob : IJob
    {
        public NativeList<GameEvent> Events;

        public void Execute()
        {
            this.Events.Add(new GameEvent());
        }
    }
}
```

Never cache `events`, `events.AsArray()`, a stream writer/reader, or an item pointer beyond the allocator generation. The owner is free to reuse that memory after the corresponding dependency completes.

## Stream-to-map capacity helper

For a singleton collection of `NativeThreadStream`, `EnsureHashMapCapacity(...)` can count queued stream items and grow a destination `NativeParallelHashMap` or `NativeParallelMultiHashMap` before a consuming job inserts them. Chain the returned handle; the helper schedules counting and resize jobs.

## Related guides

- [Collections](Collections.md) summarizes the underlying container types.
- [Jobs](Jobs.md) covers dependency chaining and Core's custom jobs.
