# Jobs
Custom jobs

## IJobParallelForDeferBatch
Job that combines [IJobParallelForDefer](https://docs.unity3d.com/Packages/com.unity.collections@2.2/api/Unity.Jobs.IJobParallelForDefer.html) and [IJobParallelForBatch](https://docs.unity3d.com/Packages/com.unity.collections@2.2/api/Unity.Jobs.IJobParallelForBatch.html)

## IJobHashMapDefer
```cs
    public unsafe interface IJobHashMapDefer
    {
        void ExecuteNext(int entryIndex, int jobIndex);
    }
```
Due to burst scheduling limitations it's not possible to schedule generic jobs from ISystem therefore this interface passes in a pointer into the job and provides a convenient extension to read it.
```cs
    [BurstCompile]
    private struct CustomJob : IJobHashMapVisitKeyValue
    {
        [ReadOnly]
        public NativeHashMap<int, Entity> Hashmap;
        
        public void ExecuteNext(byte* keys, byte* values, int entryIndex, int jobIndex)
        {
            this.Read(this.Hashmap, entryIndex, out int key, out Entity value);
```

## IJobParallelHashMapDefer
```cs
    public unsafe interface IJobParallelHashMapDefer
    {
        void ExecuteNext(int entryIndex, int jobIndex);
    }
```
Same as IJobHashMapVisitKeyValue but can iterate NativeParallelHashMap and NativeParallelMultiHashMap
```cs
    [BurstCompile]
    private struct CustomJob : IJobParallelHashMapVisitKeyValue
    {
        [ReadOnly]
        public NativeParallelHashMap<int, Entity> Hashmap;
        
        public void ExecuteNext(byte* keys, byte* values, int entryIndex, int jobIndex)
        {
            this.Read(this.Hashmap, entryIndex, out int key, out Entity value);
```
