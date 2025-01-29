// <copyright file="IJobHashMapDefer.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Jobs
{
    using System;
    using System.Runtime.CompilerServices;
    using JetBrains.Annotations;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs;
    using Unity.Jobs.LowLevel.Unsafe;

    /// <summary>
    /// A burst friendly low level hash map enumerating job.
    /// You can use <see cref="JobHashMapDefer.Read{TJob, TKey, TValue}" /> to safely get the current key/value.
    /// </summary>
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
            var jobProducer = new JobHashMapVisitKeyValueProducer<TJob>
            {
                HashMap = (HashMapWrapper*)hashMap.m_Data,
                JobData = jobData,
            };

            JobHashMapVisitKeyValueProducer<TJob>.Initialize();
            var reflectionData = JobHashMapVisitKeyValueProducer<TJob>.ReflectionData.Data;
            CollectionHelper.CheckReflectionDataCorrect<TJob>(reflectionData);

            var scheduleParams = new JobsUtility.JobScheduleParameters(
                UnsafeUtility.AddressOf(ref jobProducer), reflectionData, dependsOn, ScheduleMode.Parallel);

            void* atomicSafetyHandlePtr = null;
            // Calculate the deferred atomic safety handle before constructing JobScheduleParameters so
            // DOTS Runtime can validate the deferred list statically similar to the reflection based
            // validation in Big Unity.
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var safety = hashMap.m_Safety;
            atomicSafetyHandlePtr = UnsafeUtility.AddressOf(ref safety);
#endif

            var lengthPtr = (byte*)&hashMap.m_Data->BucketCapacity - sizeof(void*);
            return JobsUtility.ScheduleParallelForDeferArraySize(ref scheduleParams, minIndicesPerJobCount, lengthPtr, atomicSafetyHandlePtr);
        }

        public static unsafe JobHandle ScheduleParallel<TJob, TKey, TValue>(
            this TJob jobData, NativeMultiHashMap<TKey, TValue> hashMap, int minIndicesPerJobCount, JobHandle dependsOn = default)
            where TJob : unmanaged, IJobHashMapDefer
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            var jobProducer = new JobHashMapVisitKeyValueProducer<TJob>
            {
                HashMap = (HashMapWrapper*)hashMap.data,
                JobData = jobData,
            };

            JobHashMapVisitKeyValueProducer<TJob>.Initialize();
            var reflectionData = JobHashMapVisitKeyValueProducer<TJob>.ReflectionData.Data;
            CollectionHelper.CheckReflectionDataCorrect<TJob>(reflectionData);

            var scheduleParams = new JobsUtility.JobScheduleParameters(
                UnsafeUtility.AddressOf(ref jobProducer), reflectionData, dependsOn, ScheduleMode.Parallel);

            void* atomicSafetyHandlePtr = null;
            // Calculate the deferred atomic safety handle before constructing JobScheduleParameters so
            // DOTS Runtime can validate the deferred list statically similar to the reflection based
            // validation in Big Unity.
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var safety = hashMap.m_Safety;
            atomicSafetyHandlePtr = UnsafeUtility.AddressOf(ref safety);
#endif

            var lengthPtr = (byte*)&hashMap.data->BucketCapacity - sizeof(void*);
            return JobsUtility.ScheduleParallelForDeferArraySize(ref scheduleParams, minIndicesPerJobCount, lengthPtr, atomicSafetyHandlePtr);
        }

        public static unsafe JobHandle ScheduleParallel<TJob, TKey>(
            this TJob jobData, NativeHashSet<TKey> hashMap, int minIndicesPerJobCount, JobHandle dependsOn = default)
            where TJob : unmanaged, IJobHashMapDefer
            where TKey : unmanaged, IEquatable<TKey>
        {
            var jobProducer = new JobHashMapVisitKeyValueProducer<TJob>
            {
                HashMap = (HashMapWrapper*)hashMap.m_Data,
                JobData = jobData,
            };

            JobHashMapVisitKeyValueProducer<TJob>.Initialize();
            var reflectionData = JobHashMapVisitKeyValueProducer<TJob>.ReflectionData.Data;
            CollectionHelper.CheckReflectionDataCorrect<TJob>(reflectionData);

            var scheduleParams = new JobsUtility.JobScheduleParameters(
                UnsafeUtility.AddressOf(ref jobProducer), reflectionData, dependsOn, ScheduleMode.Parallel);

            void* atomicSafetyHandlePtr = null;
            // Calculate the deferred atomic safety handle before constructing JobScheduleParameters so
            // DOTS Runtime can validate the deferred list statically similar to the reflection based
            // validation in Big Unity.
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var safety = hashMap.m_Safety;
            atomicSafetyHandlePtr = UnsafeUtility.AddressOf(ref safety);
#endif

            var lengthPtr = (byte*)&hashMap.m_Data->BucketCapacity - sizeof(void*);
            return JobsUtility.ScheduleParallelForDeferArraySize(ref scheduleParams, minIndicesPerJobCount, lengthPtr, atomicSafetyHandlePtr);
        }

        /// <summary>
        /// Gathers and caches reflection data for the internal job system's managed bindings.
        /// Unity is responsible for calling this method - don't call it yourself.
        /// </summary>
        [UsedImplicitly]
        public static void EarlyJobInit<T>()
            where T : struct, IJobHashMapDefer
        {
            JobHashMapVisitKeyValueProducer<T>.Initialize();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void Read<TJob, TKey, TValue>(
            this ref TJob job, NativeHashMap<TKey, TValue> hashMap, int entryIndex, out TKey key, out TValue value)
            where TJob : unmanaged, IJobHashMapDefer
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(hashMap.m_Safety);
#endif

            key = UnsafeUtility.ReadArrayElement<TKey>(hashMap.m_Data->Keys, entryIndex);
            value = UnsafeUtility.ReadArrayElement<TValue>(hashMap.m_Data->Ptr, entryIndex);
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
            AtomicSafetyHandle.CheckReadAndThrow(hashMap.m_Safety);
#endif

            key = UnsafeUtility.ReadArrayElement<TKey>(hashMap.m_Data->Keys, entryIndex);
        }

        /// <summary> The job execution struct. </summary>
        /// <typeparam name="T"> The type of the job. </typeparam>
        internal unsafe struct JobHashMapVisitKeyValueProducer<T>
            where T : struct, IJobHashMapDefer
        {
            /// <summary> The <see cref="NativeParallelMultiHashMap{TKey,TValue}" />. </summary>
            [ReadOnly]
            [NativeDisableUnsafePtrRestriction]
            internal HashMapWrapper* HashMap;

            // ReSharper disable once StaticMemberInGenericType
            internal static readonly SharedStatic<IntPtr> ReflectionData = SharedStatic<IntPtr>.GetOrCreate<JobHashMapVisitKeyValueProducer<T>>();

            /// <summary> The job. </summary>
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

            /// <summary> Executes the job. </summary>
            /// <param name="fullData"> The job data. </param>
            /// <param name="additionalPtr"> AdditionalPtr. </param>
            /// <param name="bufferRangePatchData"> BufferRangePatchData. </param>
            /// <param name="ranges"> The job range. </param>
            /// <param name="jobIndex"> The job index. </param>
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

        internal unsafe struct HashMapWrapper
        {
            [NativeDisableUnsafePtrRestriction]
            internal byte* Ptr;

            [NativeDisableUnsafePtrRestriction]
            internal byte* Keys;

            [NativeDisableUnsafePtrRestriction]
            internal int* Next;

            [NativeDisableUnsafePtrRestriction]
            internal int* Buckets;

            private int Count;
            private int Capacity;
            private int Log2MinGrowth;
            private int BucketCapacity;
            private int AllocatedIndex;
            private int FirstFreeIdx;
            private int SizeOfTValue;
            private AllocatorManager.AllocatorHandle Allocator;
        }
    }
}
