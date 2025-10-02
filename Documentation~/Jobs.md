# Jobs

## Summary

Custom job types that extend Unity's job system for efficient iteration over hash maps and other specialized data structures.

## Job Types

### IJobForThread
Splits a linear workload across a fixed number of worker threads, giving each thread a contiguous slice of indices.

```cs
[BurstCompile]
private struct AccumulateJob : IJobForThread
{
    public NativeArray<float> Values;

    public void Execute(int index)
    {
        Values[index] = math.sqrt(Values[index]);
    }
}

// Schedule across 4 worker threads
state.Dependency = job.ScheduleParallel(Values.Length, 4, state.Dependency);
```

### IJobParallelForDeferBatch
Combines IJobParallelForDefer and IJobParallelForBatch for deferred scheduling with batch processing.

### IJobHashMapDefer
Iterates over single-threaded hash map collections.

```cs
[BurstCompile]
private struct ProcessHashMapJob : IJobHashMapDefer
{
    [ReadOnly] public NativeHashMap<int, Entity> EntityMap;
    public EntityCommandBuffer.ParallelWriter ECB;
    
    public void ExecuteNext(int entryIndex, int jobIndex)
    {
        this.Read(EntityMap, entryIndex, out int key, out Entity value);
        
        if (key > 100)
        {
            ECB.DestroyEntity(jobIndex, value);
        }
    }
}

// Schedule the job
state.Dependency = job.ScheduleParallel(myHashMap, 32, state.Dependency);
```

**Supported Collections:**
- `NativeHashMap<TKey, TValue>`
- `NativeHashSet<T>`
- `UnsafeHashMap<TKey, TValue>`
- `UnsafeHashSet<T>`

### IJobParallelHashMapDefer
Iterates over parallel hash map collections for better performance with large datasets.

```cs
[BurstCompile]
private struct ProcessParallelHashMapJob : IJobParallelHashMapDefer
{
    [ReadOnly] public NativeParallelHashMap<int, float3> PositionMap;
    [WriteOnly] public NativeArray<float3> Results;
    
    public void ExecuteNext(int entryIndex, int jobIndex)
    {
        this.Read(PositionMap, entryIndex, out int key, out float3 position);
        Results[entryIndex] = math.transform(someMatrix, position);
    }
}

// Schedule with parallel execution
state.Dependency = job.ScheduleParallel(myParallelHashMap, 32, state.Dependency);
```

**Supported Collections:**
- `NativeParallelHashMap<TKey, TValue>`
- `NativeParallelMultiHashMap<TKey, TValue>`
- `UnsafeParallelHashMap<TKey, TValue>`
- `UnsafeParallelMultiHashMap<TKey, TValue>`

## Extension Methods

```cs
// Read key-value pair from hash map
this.Read(hashMap, entryIndex, out TKey key, out TValue value);

// Read only key from hash set
this.Read(hashSet, entryIndex, out T key);

// Check if entry exists at index
bool exists = this.IsValid(hashMap, entryIndex);

// Get hash map capacity
int capacity = this.GetCapacity(hashMap);
```

## Best Practices

- Use appropriate batch sizes (typically 32-128)
- Minimize random memory access patterns
- Always use `[BurstCompile]` on job structs
- Properly chain job dependencies

## Common Patterns

### Entity Processing
```cs
[BurstCompile]
private struct EntityProcessingJob : IJobParallelHashMapDefer
{
    [ReadOnly] public NativeParallelHashMap<Entity, ComponentData> ComponentMap;
    [ReadOnly] public ComponentLookup<SomeComponent> ComponentLookup;
    
    public void ExecuteNext(int entryIndex, int jobIndex)
    {
        this.Read(ComponentMap, entryIndex, out Entity entity, out ComponentData data);
        
        if (ComponentLookup.HasComponent(entity))
        {
            var component = ComponentLookup[entity];
            // Process component...
        }
    }
}
```

### Filtering and Transformation
```cs
[BurstCompile]
private struct FilterAndTransformJob : IJobHashMapDefer
{
    [ReadOnly] public NativeHashMap<int, float> InputMap;
    [WriteOnly] public NativeList<float>.ParallelWriter OutputList;
    
    public void ExecuteNext(int entryIndex, int jobIndex)
    {
        this.Read(InputMap, entryIndex, out int key, out float value);
        
        if (value > 0.5f)
        {
            OutputList.AddNoResize(math.sqrt(value));
        }
    }
}
```

## Troubleshooting

**Job not executing:**
- Ensure the hash map is not empty
- Check that dependencies are properly set
- Verify batch size is appropriate

**Performance problems:**
- Increase batch size for better parallelization
- Avoid excessive memory allocations in jobs

**Memory issues:**
- Properly dispose of temporary collections
- Use correct allocator types (TempJob, Persistent, etc.)
