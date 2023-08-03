# Jobs
Custom jobs

## IJobParallelForDeferBatch
Job that combines [IJobParallelForDefer](https://docs.unity3d.com/Packages/com.unity.collections@2.2/api/Unity.Jobs.IJobParallelForDefer.html) and [IJobParallelForBatch](https://docs.unity3d.com/Packages/com.unity.collections@2.2/api/Unity.Jobs.IJobParallelForBatch.html)

## IJobHashMapVisitKeyValue
```cs
    public unsafe interface IJobHashMapVisitKeyValue
    {
        void ExecuteNext(byte* keys, byte* values, int entryIndex, int jobIndex);
    }
```
Due to burst scheduling limitations it's not possible to schedule generic jobs from ISystem therefore this interface passes in a pointer into the job and provides a convenient extension to read it.
```cs
    // Assumes a NativeHashMap<int, Entity>

    [BurstCompile]
    private struct CustomJob : IJobHashMapVisitKeyValue
    {
        public void ExecuteNext(byte* keys, byte* values, int entryIndex, int jobIndex)
        {
            this.Read(entryIndex, keys, values, out int key, out Entity value);
```

## IJobParallelHashMapVisitKeyValue
```cs
    public unsafe interface IJobParallelHashMapVisitKeyValue
    {
        void ExecuteNext(byte* keys, byte* values, int entryIndex, int jobIndex);
    }
```
Same as IJobHashMapVisitKeyValue but can iterate NativeParallelHashMap and NativeParallelMultiHashMap
```cs
    // Assumes a NativeParallel[Multi]HashMap<int, Entity>

    [BurstCompile]
    private struct CustomJob : IJobParallelHashMapVisitKeyValue
    {
        public void ExecuteNext(byte* keys, byte* values, int entryIndex, int jobIndex)
        {
            this.Read(entryIndex, keys, values, out int key, out Entity value);
```
