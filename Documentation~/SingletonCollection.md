# Singleton Collections

## Overview

A Many-To-One container system that allows multiple systems to write to containers while a single owning system processes them efficiently. Similar to `EntityCommandBuffer.Singleton` but with full job-based reading support and zero per-frame allocations.

**Key benefits**: Lock-free concurrent access, burst-compatible reading, automatic memory management via rewindable allocators.

## Supported Container Types

**Unity Collections:**
- `NativeArray<T>` - Fixed-size arrays
- `NativeList<T>` - Dynamic lists  
- `NativeQueue<T>` - FIFO queues
- `NativeHashMap<TKey, TValue>` - Hash tables
- `NativeParallelHashMap<TKey, TValue>` - Thread-safe hash tables
- `NativeParallelMultiHashMap<TKey, TValue>` - Thread-safe multi-value hash tables

**Core Collections:**
- `NativeThreadStream` - Lock-free event streams
- `NativeMultiHashMap<TKey, TValue>` - Multi-value hash tables

## Basic Usage

### 1. Define the Singleton Component

```csharp
public struct EventSingleton : ISingletonCollection<NativeList<GameEvent>>
{
    unsafe UnsafeList<NativeList<GameEvent>>* ISingletonCollection<NativeList<GameEvent>>.Collections { get; set; }
    Allocator ISingletonCollection<NativeList<GameEvent>>.Allocator { get; set; }
}
```

### 2. Create the Processing System

```csharp
public partial struct EventSystem : ISystem
{
    private SingletonCollectionUtil<EventSingleton, NativeList<GameEvent>> util;

    public void OnCreate(ref SystemState state) // Cannot be burst-compiled
    {
        util = new SingletonCollectionUtil<EventSingleton, NativeList<GameEvent>>(ref state);
    }

    public void OnDestroy(ref SystemState state) // Cannot be burst-compiled
    {
        util.Dispose();
    }

    [BurstCompile]
    public unsafe void OnUpdate(ref SystemState state)
    {
        var containers = util.Containers;
        
        for (int i = 0; i < containers.Length; i++)
        {
            state.Dependency = new ProcessEventsJob { Events = containers.Ptr[i] }
                .Schedule(state.Dependency);
        }

        util.ClearRewind(); // Essential: clears containers and rewinds allocator
    }

    [BurstCompile]
    private struct ProcessEventsJob : IJob
    {
        public NativeList<GameEvent> Events;

        public void Execute()
        {
            for (int i = 0; i < Events.Length; i++)
            {
                // Process event
            }
        }
    }
}
```

### 3. Writing from Other Systems

```csharp
public partial struct GameplaySystem : ISystem
{
	[BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        NativeList<GameEvent> eventList = SystemAPI.GetSingleton<EventSingleton>().CreateList<EventSingleton, GameEvent>(32);
        // Pass to job or whatever you want
    }
}
```
