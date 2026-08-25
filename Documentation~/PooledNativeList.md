# PooledNativeList

`PooledNativeList<T>` is a short-lived wrapper around `NativeList<T>` that returns its backing allocation to a thread-local pool on `Dispose()`. It is useful for immediate scratch work on the main thread or inside a job execution.

```csharp
using BovineLabs.Core.Utility;
```

## Choosing it

Use `PooledNativeList<T>` when all of these are true:

- `T` is unmanaged.
- The list is acquired, used, and disposed in one synchronous scope.
- The same executing thread performs both `Make()` and `Dispose()`.
- No scheduled work needs the list after that scope exits.

Use a normal `NativeList<T>` with an appropriate allocator when the container must cross frames, be passed to a job scheduled for later, have explicit allocator ownership, or be disposed through a `JobHandle`. For per-update system scratch allocated before scheduling, the world update allocator is usually the simpler owner.

## Basic usage

```csharp
using var pooled = PooledNativeList<int>.Make();

pooled.List.Add(42);
pooled.List.Add(24);

var sum = 0;
foreach (var value in pooled.List)
{
    sum += value;
}
```

Disposal clears the list and returns its backing memory to the current thread's pool.

## Job-local usage

Acquire the wrapper inside the job invocation that consumes it:

```csharp
[BurstCompile]
private struct BuildScratchJob : IJobFor
{
    public NativeArray<int> Results;

    public void Execute(int index)
    {
        using var scratch = PooledNativeList<int>.Make();

        var count = (index % 16) + 1;
        for (var i = 0; i < count; i++)
        {
            scratch.List.Add(i);
        }

        this.Results[index] = scratch.List.Length;
    }
}
```

This pattern is Burst-compatible and lets each worker use its own pool. Do not acquire the wrapper on the main thread, copy it into a job, and dispose it on a worker.

## Ownership rules

`PooledNativeList<T>` is a value type that represents unique ownership of a pooled list.

- Always dispose it, normally with `using var`.
- Do not copy the wrapper. Disposing two copies attempts to return the same allocation twice; checks builds detect this.
- Do not access `List` after disposal. Checks builds invalidate the returned list and throw on later use.
- Acquire and dispose on the same executing thread. The implementation indexes the pool with `JobsUtility.ThreadIndex` at each call; it does not store and restore the acquisition thread for you.
- Do not retain the wrapper or its `NativeList<T>` beyond the scope. Another caller may reuse and overwrite the same allocation immediately after disposal.

## Allocation behavior

The pool reduces repeated allocation pressure; it does not guarantee that every use allocates zero memory.

- An empty thread pool creates a new list.
- Growing a list can allocate.
- Each thread retains at most eight returned lists. Additional returned lists are disposed instead of pooled.
- The pool stores backing memory as bytes, so allocations can be reused by different unmanaged element types. Capacity is adjusted according to `sizeof(T)` when a list is checked out.
- Lists are returned empty but retain reusable capacity.

Core initializes the pool automatically in the editor and during player subsystem registration. Callers should not initialize or dispose the global pool.

## API

```csharp
public static PooledNativeList<T> Make();
public NativeList<T> List { get; }
public void Dispose();
```

## Related guides

- [Collections](Collections.md) summarizes Core and Unity collection choices.
- [Jobs](Jobs.md) covers the package's custom job scheduling APIs.
