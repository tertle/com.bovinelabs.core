# PooledNativeList

## Overview

`PooledNativeList<T>` is a high-performance, thread-safe pooling system for Unity's `NativeList<T>` collections. It's designed to minimize memory allocation overhead when working with native collections in the Unity Job System, with full support for Burst compilation. The pool is specifically built to handle the thread indexing used by Unity's job system.

## Key Features

- **Thread-Safe**: Each thread has its own dedicated pool, eliminating thread contention
- **Zero Allocation**: Reuses existing lists rather than creating new ones
- **Burst Compatible**: Works with Burst-compiled jobs for maximum performance
- **Simple API**: Get a list with a single function call, return it with `Dispose()`
- **Automatic Initialization**: The pool is automatically initialized at runtime
- **Type Conversion**: Efficiently reuses memory between different types
- **Automatic Clearing**: Lists are automatically cleared when returned to the pool

## Basic Usage

```csharp
// Get a pooled list
using var pooledList = PooledNativeList<int>.Make();

// Use the list just like a regular NativeList<int>
pooledList.List.Add(42);
pooledList.List.Add(24);

// Do some work with the list...
var sum = 0;
foreach (var item in pooledList.List)
{
    sum += item;
}

// List is automatically cleared and returned to the pool when the using block ends
```

## Using with Jobs

```csharp
[BurstCompile]
private struct MyJob : IJobFor
{
    public NativeArray<int> Results;
    
    public void Execute(int index)
    {
        // Get a pooled list
        using var pooledList = PooledNativeList<int>.Make();
        
        // Use the list
        for (var i = 0; i < 100; i++)
        {
            pooledList.List.Add(i);
        }
        
        // Store some result
        Results[index] = pooledList.List.Length;
        
        // List is automatically returned to the pool when the using block ends
    }
}

// Usage:
var results = new NativeArray<int>(64, Allocator.TempJob);
var job = new MyJob { Results = results };
var handle = job.ScheduleParallel(64, 8, default);
handle.Complete();
```

## Best Practices

1. **Always use the `using` statement** - This ensures lists are properly returned to the pool even if exceptions occur.

2. **Clear is automatic** - The list is automatically cleared when returned to the pool, so you don't need to clear it yourself.

3. **Thread affinity** - Lists must be returned on the same thread they were obtained from. The list knows which thread it belongs to and will be returned to that thread's pool automatically.

4. **Type conversion** - When obtaining a list of a different type than what was previously in the pool, the capacity is automatically adjusted based on the size of the type.

5. **No external dependencies** - The pool is globally accessible without requiring you to create or manage any instances.

6. **Short-lived usage** - Get a list, use it, dispose it. Don't hold onto pooled lists for long periods.

7. **Only store unmanaged types** - Like standard `NativeList<T>`, only unmanaged types are supported.

## API Reference

### PooledNativeList<T>

```csharp
// Create a new pooled list
public static PooledNativeList<T> Make();

// Access the internal NativeList<T>
public NativeList<T> List { get; }

// Return the list to the pool
public void Dispose();
```

### Complete Example

```csharp
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using BovineLabs.Core.Utility;

public class PooledNativeListExample : MonoBehaviour
{
    private void Start()
    {
        // Single-threaded example
        using (var numbers = PooledNativeList<int>.Make())
        {
            for (int i = 0; i < 10; i++)
            {
                numbers.List.Add(i);
            }
            
            Debug.Log($"List contains {numbers.List.Length} items");
        }
        
        // Multi-threaded example
        var jobCount = 10;
        var results = new NativeArray<int>(jobCount, Allocator.TempJob);
        
        var handle = new ExampleJob
        {
            Results = results
        }.ScheduleParallel(jobCount, 1, default);
        
        handle.Complete();
        
        for (int i = 0; i < jobCount; i++)
        {
            Debug.Log($"Job {i} processed {results[i]} items");
        }
        
        results.Dispose();
    }
    
    [BurstCompile]
    private struct ExampleJob : IJobFor
    {
        public NativeArray<int> Results;
        
        public void Execute(int index)
        {
            using var ints = PooledNativeList<int>.Make();
            using var floats = PooledNativeList<float>.Make();
            
            int count = index + 1;
            
            for (int i = 0; i < count; i++)
            {
                ints.List.Add(i);
                floats.List.Add(i * 0.5f);
            }
            
            Results[index] = ints.List.Length + floats.List.Length;
        }
    }
}
```