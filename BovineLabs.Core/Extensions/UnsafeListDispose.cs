namespace BovineLabs.Core.Extensions
{
    using Unity.Burst;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs;

    [BurstCompile]
    public unsafe struct UnsafeListDispose : IJob
    {
        [NativeDisableUnsafePtrRestriction]
        private void* ListData;

        public static JobHandle Dispose<T>(UnsafeList<T>* list, JobHandle handle)
            where T : unmanaged
        {
            return new UnsafeListDispose { ListData = (void*)list }.Schedule(handle);
        }

        public void Execute()
        {
            var listData = (UnsafeList<int>*)this.ListData;
            UnsafeList<int>.Destroy(listData);
        }
    }
}
