// <copyright file="IJobParallelHashMapDefer.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Jobs
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.CompilerServices;
    using JetBrains.Annotations;
    using Unity.Burst;
    using Unity.Burst.CompilerServices;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs;
    using Unity.Jobs.LowLevel.Unsafe;

    /// <summary>
    /// A burst friendly low level hash map enumerating job.
    /// You can use <see cref="JobParallelHashMapDefer.Read{TJob, TKey, TValue}" /> to safely get the current key/value.
    /// </summary>
    [JobProducerType(typeof(JobParallelHashMapDefer.JobParallelHashMapVisitKeyValueProducer<>))]
    public interface IJobParallelHashMapDefer
    {
        void OnWorkerBegin()
        {
        }

        void OnWorkerEnd()
        {
        }

        void ExecuteNext(int entryIndex, int jobIndex);

        void OnBucketEnd()
        {
        }
    }

    public static class JobParallelHashMapDefer
    {
        public static unsafe JobHandle ScheduleParallel<TJob, TKey, TValue>(
            this TJob jobData, NativeParallelMultiHashMap<TKey, TValue> hashMap, int minIndicesPerJobCount, JobHandle dependsOn = default)
            where TJob : unmanaged, IJobParallelHashMapDefer
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            // Calculate the deferred atomic safety handle before constructing JobScheduleParameters so
            // DOTS Runtime can validate the deferred list statically similar to the reflection based
            // validation in Big Unity.
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var safety = hashMap.m_Safety;
            var atomicSafetyHandlePtr = UnsafeUtility.AddressOf(ref safety);
#else
            void* atomicSafetyHandlePtr = null;
#endif

            return ScheduleParallelInternal(jobData, hashMap.m_MultiHashMapData.m_Buffer, atomicSafetyHandlePtr, minIndicesPerJobCount, dependsOn);
        }

        public static unsafe JobHandle ScheduleParallel<TJob, TKey, TValue>(
            this TJob jobData, NativeParallelMultiHashMap<TKey, TValue>.ReadOnly hashMap, int minIndicesPerJobCount, JobHandle dependsOn = default)
            where TJob : unmanaged, IJobParallelHashMapDefer
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            // Calculate the deferred atomic safety handle before constructing JobScheduleParameters so
            // DOTS Runtime can validate the deferred list statically similar to the reflection based
            // validation in Big Unity.
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var safety = UnsafeUtility.As<NativeParallelMultiHashMap<TKey, TValue>.ReadOnly, NativeParallelMultiHashMapReadOnly<TKey, TValue>>(ref hashMap)
                .Safety;

            var atomicSafetyHandlePtr = UnsafeUtility.AddressOf(ref safety);
#else
            void* atomicSafetyHandlePtr = null;
#endif

            return ScheduleParallelInternal(jobData, hashMap.m_MultiHashMapData.m_Buffer, atomicSafetyHandlePtr, minIndicesPerJobCount, dependsOn);
        }

        public static unsafe JobHandle ScheduleParallel<TJob, TKey, TValue>(
            this TJob jobData, NativeParallelHashMap<TKey, TValue> hashMap, int minIndicesPerJobCount, JobHandle dependsOn = default)
            where TJob : unmanaged, IJobParallelHashMapDefer
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            // Calculate the deferred atomic safety handle before constructing JobScheduleParameters so
            // DOTS Runtime can validate the deferred list statically similar to the reflection based
            // validation in Big Unity.
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var safety = hashMap.m_Safety;
            var atomicSafetyHandlePtr = UnsafeUtility.AddressOf(ref safety);
#else
            void* atomicSafetyHandlePtr = null;
#endif

            return ScheduleParallelInternal(jobData, hashMap.m_HashMapData.m_Buffer, atomicSafetyHandlePtr, minIndicesPerJobCount, dependsOn);
        }

        public static unsafe JobHandle ScheduleParallel<TJob, TKey, TValue>(
            this TJob jobData, NativeParallelHashMap<TKey, TValue>.ReadOnly hashMap, int minIndicesPerJobCount, JobHandle dependsOn = default)
            where TJob : unmanaged, IJobParallelHashMapDefer
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            // Calculate the deferred atomic safety handle before constructing JobScheduleParameters so
            // DOTS Runtime can validate the deferred list statically similar to the reflection based
            // validation in Big Unity.
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var safety = UnsafeUtility.As<NativeParallelHashMap<TKey, TValue>.ReadOnly, NativeParallelHashMapReadOnly<TKey, TValue>>(ref hashMap).Safety;
            var atomicSafetyHandlePtr = UnsafeUtility.AddressOf(ref safety);
#else
            void* atomicSafetyHandlePtr = null;
#endif

            return ScheduleParallelInternal(jobData, hashMap.m_HashMapData.m_Buffer, atomicSafetyHandlePtr, minIndicesPerJobCount, dependsOn);
        }

        private static unsafe JobHandle ScheduleParallelInternal<TJob>(
            TJob jobData, UnsafeParallelHashMapData* hashMap, void* atomicSafetyHandlePtr, int minIndicesPerJobCount, JobHandle dependsOn)
            where TJob : unmanaged, IJobParallelHashMapDefer
        {
            var jobProducer = new JobParallelHashMapVisitKeyValueProducer<TJob>
            {
                HashMap = hashMap,
                JobData = jobData,
            };

            JobParallelHashMapVisitKeyValueProducer<TJob>.Initialize();
            var reflectionData = JobParallelHashMapVisitKeyValueProducer<TJob>.ReflectionData.Data;
            CollectionHelper.CheckReflectionDataCorrect<TJob>(reflectionData);

            var scheduleParams = new JobsUtility.JobScheduleParameters(
                UnsafeUtility.AddressOf(ref jobProducer), reflectionData, dependsOn, ScheduleMode.Parallel);

            var lengthPtr = (byte*)&hashMap->bucketCapacityMask - sizeof(void*);

            return JobsUtility.ScheduleParallelForDeferArraySize(ref scheduleParams, minIndicesPerJobCount, lengthPtr, atomicSafetyHandlePtr);
        }

        /// <summary>
        /// Gathers and caches reflection data for the internal job system's managed bindings.
        /// Unity is responsible for calling this method - don't call it yourself.
        /// </summary>
        [UsedImplicitly]
        public static void EarlyJobInit<T>()
            where T : struct, IJobParallelHashMapDefer
        {
            JobParallelHashMapVisitKeyValueProducer<T>.Initialize();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void Read<TJob, TKey, TValue>(
            this ref TJob job, NativeParallelHashMap<TKey, TValue> hashMap, int entryIndex, out TKey key, out TValue value)
            where TJob : unmanaged, IJobParallelHashMapDefer
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(hashMap.m_Safety);
#endif

            key = UnsafeUtility.ReadArrayElement<TKey>(hashMap.m_HashMapData.m_Buffer->keys, entryIndex);
            value = UnsafeUtility.ReadArrayElement<TValue>(hashMap.m_HashMapData.m_Buffer->values, entryIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void Read<TJob, TKey, TValue>(
            this ref TJob job, NativeParallelHashMap<TKey, TValue>.ReadOnly hashMap, int entryIndex, out TKey key, out TValue value)
            where TJob : unmanaged, IJobParallelHashMapDefer
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var re = UnsafeUtility.As<NativeParallelHashMap<TKey, TValue>.ReadOnly, NativeParallelHashMapReadOnly<TKey, TValue>>(ref hashMap);
            AtomicSafetyHandle.CheckReadAndThrow(re.Safety);
#endif

            key = UnsafeUtility.ReadArrayElement<TKey>(hashMap.m_HashMapData.m_Buffer->keys, entryIndex);
            value = UnsafeUtility.ReadArrayElement<TValue>(hashMap.m_HashMapData.m_Buffer->values, entryIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void Read<TJob, TKey, TValue>(
            this ref TJob job, NativeParallelMultiHashMap<TKey, TValue> hashMap, int entryIndex, out TKey key, out TValue value)
            where TJob : unmanaged, IJobParallelHashMapDefer
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(hashMap.m_Safety);
#endif

            key = UnsafeUtility.ReadArrayElement<TKey>(hashMap.m_MultiHashMapData.m_Buffer->keys, entryIndex);
            value = UnsafeUtility.ReadArrayElement<TValue>(hashMap.m_MultiHashMapData.m_Buffer->values, entryIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void Read<TJob, TKey, TValue>(
            this ref TJob job, NativeParallelMultiHashMap<TKey, TValue>.ReadOnly hashMap, int entryIndex, out TKey key, out TValue value)
            where TJob : unmanaged, IJobParallelHashMapDefer
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var re = UnsafeUtility.As<NativeParallelMultiHashMap<TKey, TValue>.ReadOnly, NativeParallelMultiHashMapReadOnly<TKey, TValue>>(ref hashMap);
            AtomicSafetyHandle.CheckReadAndThrow(re.Safety);
#endif

            key = UnsafeUtility.ReadArrayElement<TKey>(hashMap.m_MultiHashMapData.m_Buffer->keys, entryIndex);
            value = UnsafeUtility.ReadArrayElement<TValue>(hashMap.m_MultiHashMapData.m_Buffer->values, entryIndex);
        }

        /// <summary> The job execution struct. </summary>
        /// <typeparam name="T"> The type of the job. </typeparam>
        internal unsafe struct JobParallelHashMapVisitKeyValueProducer<T>
            where T : struct, IJobParallelHashMapDefer
        {
            /// <summary> The <see cref="NativeParallelMultiHashMap{TKey,TValue}" />. </summary>
            [ReadOnly]
            [NativeDisableUnsafePtrRestriction]
            internal UnsafeParallelHashMapData* HashMap;

            // ReSharper disable once StaticMemberInGenericType
            internal static readonly SharedStatic<IntPtr> ReflectionData = SharedStatic<IntPtr>.GetOrCreate<JobParallelHashMapVisitKeyValueProducer<T>>();

            /// <summary> The job. </summary>
            internal T JobData;

            private delegate void ExecuteJobFunction(
                ref JobParallelHashMapVisitKeyValueProducer<T> producer, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex);

            [BurstDiscard]
            internal static void Initialize()
            {
                if (ReflectionData.Data == IntPtr.Zero)
                {
                    ReflectionData.Data = JobsUtility.CreateJobReflectionData(
                        typeof(JobParallelHashMapVisitKeyValueProducer<T>), typeof(T), (ExecuteJobFunction)Execute);
                }
            }

            /// <summary> Executes the job. </summary>
            /// <param name="jobWrapper"> The job data. </param>
            /// <param name="additionalPtr"> AdditionalPtr. </param>
            /// <param name="bufferRangePatchData"> BufferRangePatchData. </param>
            /// <param name="ranges"> The job range. </param>
            /// <param name="jobIndex"> The job index. </param>
            [SuppressMessage("ReSharper", "MemberCanBePrivate.Global", Justification = "Required by burst.")]
            internal static void Execute(
                ref JobParallelHashMapVisitKeyValueProducer<T> jobWrapper, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges,
                int jobIndex)
            {
                // If we have 0 capacity, the mask is -1
                if (ranges.TotalIterationCount == -1)
                {
                    return;
                }

                var executed = false;

                while (true)
                {
                    if (!JobsUtility.GetWorkStealingRange(ref ranges, jobIndex, out var begin, out var end))
                    {
                        break;
                    }

                    if (!executed)
                    {
                        executed = true;
                        jobWrapper.JobData.OnWorkerBegin();
                    }

                    // We need bucket capacity but the parallel version stores it as the mask so we need to +1 for the last bucket
                    if (Hint.Unlikely(end == jobWrapper.HashMap->bucketCapacityMask))
                    {
                        end++;
                    }

                    var buckets = (int*)jobWrapper.HashMap->buckets;
                    var nextPtrs = (int*)jobWrapper.HashMap->next;

                    for (var i = begin; i < end; i++)
                    {
                        var entryIndex = buckets[i];
                        var anyValid = false;

                        while (entryIndex != -1)
                        {
                            anyValid = true;
                            jobWrapper.JobData.ExecuteNext(entryIndex, jobIndex);
                            entryIndex = nextPtrs[entryIndex];
                        }

                        if (anyValid)
                        {
                            jobWrapper.JobData.OnBucketEnd();
                        }
                    }
                }

                if (executed)
                {
                    jobWrapper.JobData.OnWorkerEnd();
                }
            }
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private struct NativeParallelMultiHashMapReadOnly<TKey, TValue>
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            public UnsafeParallelHashMap<TKey, TValue> HashMapData;
            public AtomicSafetyHandle Safety;
        }

        private struct NativeParallelHashMapReadOnly<TKey, TValue>
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            public UnsafeParallelMultiHashMap<TKey, TValue> HashMapData;
            public AtomicSafetyHandle Safety;
        }
#endif
    }
}
