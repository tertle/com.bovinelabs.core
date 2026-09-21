namespace BovineLabs.Core.Extensions
{
    using Unity.Burst;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs;

    [BurstCompile]
    public unsafe struct UnsafeListDispose : IJob
    {
        [NativeDisableUnsafePtrRestriction]
        private void* _listData;

        public static JobHandle Dispose<T>(UnsafeList<T>* list, JobHandle handle)
            where T : unmanaged
        {
            return new UnsafeListDispose { _listData = (void*)list }.Schedule(handle);
        }

        public void Execute()
        {
            var listData = (UnsafeList<int>*)_listData;
            UnsafeList<int>.Destroy(listData);
        }
    }
}
