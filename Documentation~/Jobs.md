# Jobs

Core adds several low-level Burst-compatible job producers for workloads that Unity's standard jobs do not cover directly.

```csharp
using BovineLabs.Core.Collections;
using BovineLabs.Core.Jobs;
```

## Choose a job type

| Requirement | Job interface | Execute shape |
|---|---|---|
| Divide a known range across a fixed number of workers | `IJobForThread` | `Execute(int index)` |
| Batch a length that is produced by an earlier job | `IJobParallelForDeferBatch` | `Execute(int startIndex, int count)` |
| Visit `NativeHashMap`, `NativeMultiHashMap`, or `NativeHashSet` entries | `IJobHashMapDefer` | `ExecuteNext(int entryIndex, int jobIndex)` |
| Visit `NativeParallelHashMap` or `NativeParallelMultiHashMap` entries | `IJobParallelHashMapDefer` | `ExecuteNext(int entryIndex, int jobIndex)` |
| Run setup and teardown once per worker around chunk processing | `IJobChunkWorkerBeginEnd` | Worker hooks plus the `IJobChunk` execute signature |

All scheduling methods return a `JobHandle`. Chain it into the owner's dependency and keep every input container valid until that handle completes.

## IJobForThread

`IJobForThread` divides `arrayLength` into contiguous slices for the requested number of workers, then invokes `Execute` once per index.

```csharp
[BurstCompile]
private struct SquareJob : IJobForThread
{
    public NativeArray<float> Values;

    public void Execute(int index)
    {
        this.Values[index] *= this.Values[index];
    }
}

var job = new SquareJob { Values = values };
dependency = job.ScheduleParallel(values.Length, threadCount: 4, dependency);
```

The scheduler clamps `threadCount` to at least one. Choose a fixed count only when contiguous per-worker slices are useful; use `IJobFor` for ordinary work-stealing iteration.

## IJobParallelForDeferBatch

Use this when a dependency determines the final list length. The scheduled job must also contain the same list used for deferred scheduling.

```csharp
[BurstCompile]
private struct ScaleBatchJob : IJobParallelForDeferBatch
{
    public NativeList<float> Values;

    public void Execute(int startIndex, int count)
    {
        var end = startIndex + count;

        for (var i = startIndex; i < end; i++)
        {
            this.Values[i] *= 2;
        }
    }
}

dependency = new ScaleBatchJob
{
    Values = values,
}.ScheduleParallel(values, innerloopBatchCount: 64, dependency);
```

Supported count sources include:

- `NativeList<T>` with `Schedule`, `ScheduleParallel`, or `ScheduleParallelByRef`.
- `NativeReference<int>` with `ScheduleParallel`.
- An unsafe count pointer with `ScheduleParallel` or `ScheduleParallelByRef`.

Prefer the `NativeList<T>` overload. Pointer count overloads bypass container safety and require the pointed-to storage to remain valid through scheduling and execution.

## IJobHashMapDefer

This producer walks occupied entries without first copying keys or values.

Supported containers:

- `NativeHashMap<TKey, TValue>`
- `NativeMultiHashMap<TKey, TValue>`
- `NativeHashSet<TKey>`

Each has `Schedule` and `ScheduleParallel` overloads. The collection is read during traversal and must not be resized, cleared, or otherwise mutated until the job completes.

```csharp
[BurstCompile]
private struct CollectPositiveJob : IJobHashMapDefer
{
    [ReadOnly]
    public NativeHashMap<int, float> Input;

    public NativeQueue<float>.ParallelWriter Output;

    public void ExecuteNext(int entryIndex, int jobIndex)
    {
        this.Read(this.Input, entryIndex, out var key, out var value);

        if (key >= 0 && value > 0)
        {
            this.Output.Enqueue(value);
        }
    }
}

dependency = new CollectPositiveJob
{
    Input = input,
    Output = output.AsParallelWriter(),
}.ScheduleParallel(input, minIndicesPerJobCount: 64, dependency);
```

For a hash set, use the matching overload:

```csharp
this.Read(this.InputSet, entryIndex, out var key);
```

`entryIndex` is an internal hash-map slot, not a dense zero-to-`Count` result index. Use it only with `Read` for the same collection unless the destination is sized for the source's full internal capacity.

## IJobParallelHashMapDefer

This variant supports:

- `NativeParallelHashMap<TKey, TValue>` and its `ReadOnly` view.
- `NativeParallelMultiHashMap<TKey, TValue>` and its `ReadOnly` view.

It exposes only `ScheduleParallel`. Read an entry with `this.Read(map, entryIndex, out key, out value)`.

`IJobParallelHashMapDefer` also has optional default interface hooks:

```csharp
void OnWorkerBegin();
void ExecuteNext(int entryIndex, int jobIndex);
void OnBucketEnd();
void OnWorkerEnd();
```

`OnWorkerBegin` and `OnWorkerEnd` run only for workers that receive work. `OnBucketEnd` runs after a non-empty bucket. Keep hook state inside the job or in correctly partitioned native storage; a copied job struct is not shared managed state.

## IJobChunkWorkerBeginEnd

Use `IJobChunkWorkerBeginEnd` when chunk processing needs one setup and teardown call per participating worker.

```csharp
[BurstCompile]
private struct ProcessChunksJob : IJobChunkWorkerBeginEnd
{
    [ReadOnly]
    public ComponentTypeHandle<InputData> InputHandle;

    public void OnWorkerBegin()
    {
    }

    public void Execute(
        in ArchetypeChunk chunk,
        int unfilteredChunkIndex,
        bool useEnabledMask,
        in v128 chunkEnabledMask)
    {
        var inputs = chunk.GetNativeArray(ref this.InputHandle);
        var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);

        while (enumerator.NextEntityIndex(out var entityIndex))
        {
            var input = inputs[entityIndex];
        }
    }

    public void OnWorkerEnd()
    {
    }
}

dependency = new ProcessChunksJob
{
    InputHandle = inputHandle,
}.ScheduleParallel(query, dependency);
```

Available forms are `Schedule`, `ScheduleByRef`, `ScheduleParallel`, `ScheduleParallelByRef`, `Run`, and `RunByRef`. Prefer scheduled forms in systems and update cached handles before constructing the job.

## Safety and performance

- Mark read-only inputs with `[ReadOnly]` and use parallel writers for shared outputs.
- Do not assume hash-map entry order is deterministic.
- Do not mutate a traversed hash map while a defer job is using its bucket and next-pointer storage.
- Treat `minIndicesPerJobCount` as work-stealing granularity, not a promised number of calls.
- Store the returned handle. A fire-and-forget schedule can race container disposal or later writes.
- Generic closed job types normally receive generated early initialization. Register unusual generic specializations when Unity cannot discover them.

See [Collections](Collections.md) for the container inventory and [Iterators](Iterators.md) for query, chunk-mask, and dynamic-map iteration.
