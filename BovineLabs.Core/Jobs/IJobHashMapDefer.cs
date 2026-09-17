namespace BovineLabs.Core.Jobs
{
    using System;
    using System.Runtime.CompilerServices;
    using BovineLabs.Core.Internal;
    using JetBrains.Annotations;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs;
    using Unity.Jobs.LowLevel.Unsafe;

    [JobProducerType(typeof(JobHashMapDefer.JobHashMapVisitKeyValueProducer<>))]
    public interface IJobHashMapDefer
    {
        void ExecuteNext(int entryIndex, int jobIndex);
    }

    public static class JobHashMapDefer
    {
        public static unsafe JobHandle ScheduleParallel<TJob, TKey, TValue>(
            this TJob jobData, NativeHashMap<TKey, TValue> hashMap, int minIndicesPerJobCount, JobHandle dependsOn = default)
            where TJob : unmanaged, IJobHashMapDefer
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            void* atomicSafetyHandlePtr = null;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var safety = hashMap.GetSafety();
            atomicSafetyHandlePtr = UnsafeUtility.AddressOf(ref safety);
#endif
            return ScheduleInternal(jobData, (HashMapHelper<byte>*)hashMap.GetData(), minIndicesPerJobCount, dependsOn, ScheduleMode.Parallel,
                atomicSafetyHandlePtr);
        }

        public static unsafe JobHandle Schedule<TJob, TKey, TValue>(
            this TJob jobData, NativeHashMap<TKey, TValue> hashMap, int minIndicesPerJobCount, JobHandle dependsOn = default)
            where TJob : unmanaged, IJobHashMapDefer
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            void* atomicSafetyHandlePtr = null;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var safety = hashMap.GetSafety();
            atomicSafetyHandlePtr = UnsafeUtility.AddressOf(ref safety);
#endif

            return ScheduleInternal(jobData, (HashMapHelper<byte>*)hashMap.GetData(), minIndicesPerJobCount, dependsOn, ScheduleMode.Single,
                atomicSafetyHandlePtr);
        }

        public static unsafe JobHandle ScheduleParallel<TJob, TKey, TValue>(
            this TJob jobData, NativeMultiHashMap<TKey, TValue> hashMap, int minIndicesPerJobCount, JobHandle dependsOn = default)
            where TJob : unmanaged, IJobHashMapDefer
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            void* atomicSafetyHandlePtr = null;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var safety = hashMap.m_Safety;
            atomicSafetyHandlePtr = UnsafeUtility.AddressOf(ref safety);
#endif

            return ScheduleInternal(jobData, (HashMapHelper<byte>*)hashMap.data, minIndicesPerJobCount, dependsOn, ScheduleMode.Parallel,
                atomicSafetyHandlePtr);
        }

        public static unsafe JobHandle Schedule<TJob, TKey, TValue>(
            this TJob jobData, NativeMultiHashMap<TKey, TValue> hashMap, int minIndicesPerJobCount, JobHandle dependsOn = default)
            where TJob : unmanaged, IJobHashMapDefer
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            void* atomicSafetyHandlePtr = null;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var safety = hashMap.m_Safety;
            atomicSafetyHandlePtr = UnsafeUtility.AddressOf(ref safety);
#endif

            return ScheduleInternal(jobData, (HashMapHelper<byte>*)hashMap.data, minIndicesPerJobCount, dependsOn, ScheduleMode.Single, atomicSafetyHandlePtr);
        }

        public static unsafe JobHandle ScheduleParallel<TJob, TKey>(
            this TJob jobData, NativeHashSet<TKey> hashMap, int minIndicesPerJobCount, JobHandle dependsOn = default)
            where TJob : unmanaged, IJobHashMapDefer
            where TKey : unmanaged, IEquatable<TKey>
        {
            void* atomicSafetyHandlePtr = null;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var safety = hashMap.GetSafety();
            atomicSafetyHandlePtr = UnsafeUtility.AddressOf(ref safety);
#endif

            return ScheduleInternal(jobData, (HashMapHelper<byte>*)hashMap.GetData(), minIndicesPerJobCount, dependsOn, ScheduleMode.Parallel,
                atomicSafetyHandlePtr);
        }

        public static unsafe JobHandle Schedule<TJob, TKey>(
            this TJob jobData, NativeHashSet<TKey> hashMap, int minIndicesPerJobCount, JobHandle dependsOn = default)
            where TJob : unmanaged, IJobHashMapDefer
            where TKey : unmanaged, IEquatable<TKey>
        {
            void* atomicSafetyHandlePtr = null;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var safety = hashMap.GetSafety();
            atomicSafetyHandlePtr = UnsafeUtility.AddressOf(ref safety);
#endif

            return ScheduleInternal(jobData, (HashMapHelper<byte>*)hashMap.GetData(), minIndicesPerJobCount, dependsOn, ScheduleMode.Single,
                atomicSafetyHandlePtr);
        }

        /// <summary>
        /// Called by Unity for job reflection initialization; do not invoke directly.
        /// </summary>
        [UsedImplicitly]
        public static void EarlyJobInit<T>()
            where T : struct, IJobHashMapDefer
        {
            JobHashMapVisitKeyValueProducer<T>.Initialize();
        }

        private static unsafe JobHandle ScheduleInternal<TJob>(
            TJob jobData, HashMapHelper<byte>* hashMap, int minIndicesPerJobCount, JobHandle dependsOn, ScheduleMode scheduleMode, void* atomicSafetyHandlePtr)
            where TJob : unmanaged, IJobHashMapDefer
        {
            var jobProducer = new JobHashMapVisitKeyValueProducer<TJob>
            {
                HashMap = hashMap,
                JobData = jobData,
            };

            JobHashMapVisitKeyValueProducer<TJob>.Initialize();
            var reflectionData = JobHashMapVisitKeyValueProducer<TJob>.ReflectionData.Data;
            CollectionHelper.CheckReflectionDataCorrect<TJob>(reflectionData);

            var scheduleParams = new JobsUtility.JobScheduleParameters(
                UnsafeUtility.AddressOf(ref jobProducer), reflectionData, dependsOn, scheduleMode);

            var lengthPtr = (byte*)&hashMap->BucketCapacity - sizeof(void*);
            return JobsUtility.ScheduleParallelForDeferArraySize(ref scheduleParams, minIndicesPerJobCount, lengthPtr, atomicSafetyHandlePtr);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void Read<TJob, TKey, TValue>(
            this ref TJob job, NativeHashMap<TKey, TValue> hashMap, int entryIndex, out TKey key, out TValue value)
            where TJob : unmanaged, IJobHashMapDefer
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(hashMap.GetSafety());
#endif

            var data = hashMap.GetData();
            key = UnsafeUtility.ReadArrayElement<TKey>(data->Keys, entryIndex);
            value = UnsafeUtility.ReadArrayElement<TValue>(data->Ptr, entryIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void Read<TJob, TKey, TValue>(
            this ref TJob job, NativeMultiHashMap<TKey, TValue> hashMap, int entryIndex, out TKey key, out TValue value)
            where TJob : unmanaged, IJobHashMapDefer
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(hashMap.m_Safety);
#endif

            key = UnsafeUtility.ReadArrayElement<TKey>(hashMap.data->Keys, entryIndex);
            value = UnsafeUtility.ReadArrayElement<TValue>(hashMap.data->Ptr, entryIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void Read<TJob, TKey>(this ref TJob job, NativeHashSet<TKey> hashMap, int entryIndex, out TKey key)
            where TJob : unmanaged, IJobHashMapDefer
            where TKey : unmanaged, IEquatable<TKey>
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(hashMap.GetSafety());
#endif

            key = UnsafeUtility.ReadArrayElement<TKey>(hashMap.GetData()->Keys, entryIndex);
        }

        internal unsafe struct JobHashMapVisitKeyValueProducer<T>
            where T : struct, IJobHashMapDefer
        {
            [ReadOnly]
            [NativeDisableUnsafePtrRestriction]
            internal HashMapHelper<byte>* HashMap;

            // ReSharper disable once StaticMemberInGenericType
            internal static readonly SharedStatic<IntPtr> ReflectionData = SharedStatic<IntPtr>.GetOrCreate<JobHashMapVisitKeyValueProducer<T>>();

            internal T JobData;

            private delegate void ExecuteJobFunction(
                ref JobHashMapVisitKeyValueProducer<T> producer, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex);

            [BurstDiscard]
            internal static void Initialize()
            {
                if (ReflectionData.Data == IntPtr.Zero)
                {
                    ReflectionData.Data =
                        JobsUtility.CreateJobReflectionData(typeof(JobHashMapVisitKeyValueProducer<T>), typeof(T), (ExecuteJobFunction)Execute);
                }
            }

            internal static void Execute(
                ref JobHashMapVisitKeyValueProducer<T> fullData, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex)
            {
                while (true)
                {
                    if (!JobsUtility.GetWorkStealingRange(ref ranges, jobIndex, out var begin, out var end))
                    {
                        return;
                    }

                    var buckets = fullData.HashMap->Buckets;
                    var nextPtrs = fullData.HashMap->Next;

                    for (var i = begin; i < end; i++)
                    {
                        var entryIndex = buckets[i];

                        while (entryIndex != -1)
                        {
                            fullData.JobData.ExecuteNext(entryIndex, jobIndex);
                            entryIndex = nextPtrs[entryIndex];
                        }
                    }
                }
            }
        }

    }
}
