// <copyright file="IJobHashMapVisitKeyValue.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Jobs
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs;
    using Unity.Jobs.LowLevel.Unsafe;

    /// <summary>
    /// A burst friendly low level hash map enumerating job.
    /// You can use <see cref="JobHashMapVisitKeyValue.Read{TJob, TKey, TValue}" /> to safely get the current key/value.
    /// </summary>
    [JobProducerType(typeof(JobHashMapVisitKeyValue.JobJobHashMapVisitKeyValueProducer<>))]
    public unsafe interface IJobHashMapVisitKeyValue
    {
        void ExecuteNext(byte* keys, byte* values, int entryIndex);
    }

    public static class JobHashMapVisitKeyValue
    {
        public static unsafe JobHandle ScheduleParallel<TJob, TKey, TValue>(
            this TJob jobData,
            NativeMultiHashMap<TKey, TValue> hashMap,
            int minIndicesPerJobCount,
            JobHandle dependsOn = default)
            where TJob : unmanaged, IJobHashMapVisitKeyValue
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            var jobProducer = new JobJobHashMapVisitKeyValueProducer<TJob>
            {
                HashMap = hashMap.m_MultiHashMapData.m_Buffer,
                JobData = jobData,
            };

            JobJobHashMapVisitKeyValueProducer<TJob>.Initialize();
            var reflectionData = JobJobHashMapVisitKeyValueProducer<TJob>.ReflectionData.Data;
            CollectionHelper.CheckReflectionDataCorrect<TJob>(reflectionData);

            var scheduleParams = new JobsUtility.JobScheduleParameters(
                UnsafeUtility.AddressOf(ref jobProducer),
                reflectionData,
                dependsOn,
                ScheduleMode.Parallel);

            return JobsUtility.ScheduleParallelFor(ref scheduleParams, hashMap.GetUnsafeBucketData().bucketCapacityMask + 1, minIndicesPerJobCount);
        }

        public static unsafe JobHandle ScheduleParallel<TJob, TKey, TValue>(
            this TJob jobData,
            NativeParallelHashMap<TKey, TValue> hashMap,
            int minIndicesPerJobCount,
            JobHandle dependsOn = default)
            where TJob : unmanaged, IJobHashMapVisitKeyValue
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            var jobProducer = new JobJobHashMapVisitKeyValueProducer<TJob>
            {
                HashMap = hashMap.m_HashMapData.m_Buffer,
                JobData = jobData,
            };

            JobJobHashMapVisitKeyValueProducer<TJob>.Initialize();
            var reflectionData = JobJobHashMapVisitKeyValueProducer<TJob>.ReflectionData.Data;
            CollectionHelper.CheckReflectionDataCorrect<TJob>(reflectionData);

            var scheduleParams = new JobsUtility.JobScheduleParameters(
                UnsafeUtility.AddressOf(ref jobProducer),
                reflectionData,
                dependsOn,
                ScheduleMode.Parallel);

            return JobsUtility.ScheduleParallelFor(ref scheduleParams, hashMap.GetUnsafeBucketData().bucketCapacityMask + 1, minIndicesPerJobCount);
        }

        public static void EarlyJobInit<T>()
            where T : struct, IJobHashMapVisitKeyValue
        {
            JobJobHashMapVisitKeyValueProducer<T>.Initialize();
        }

        public static unsafe void Read<TJob, TKey, TValue>(this ref TJob _, int entryIndex, byte* keys, byte* values, out TKey key, out TValue value)
            where TJob : unmanaged, IJobHashMapVisitKeyValue
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            key = UnsafeUtility.ReadArrayElement<TKey>(keys, entryIndex);
            value = UnsafeUtility.ReadArrayElement<TValue>(values, entryIndex);
        }

        /// <summary> The job execution struct. </summary>
        /// <typeparam name="T"> The type of the job. </typeparam>
        internal unsafe struct JobJobHashMapVisitKeyValueProducer<T>
            where T : struct, IJobHashMapVisitKeyValue
        {
            /// <summary> The <see cref="NativeMultiHashMap{TKey,TValue}" />. </summary>
            [ReadOnly]
            public UnsafeParallelHashMapData* HashMap;

            // ReSharper disable once StaticMemberInGenericType
            internal static readonly SharedStatic<IntPtr> ReflectionData = SharedStatic<IntPtr>.GetOrCreate<JobJobHashMapVisitKeyValueProducer<T>>();

            /// <summary> The job. </summary>
            internal T JobData;

            private delegate void ExecuteJobFunction(
                ref JobJobHashMapVisitKeyValueProducer<T> producer,
                IntPtr additionalPtr,
                IntPtr bufferRangePatchData,
                ref JobRanges ranges,
                int jobIndex);

            [BurstDiscard]
            internal static void Initialize()
            {
                if (ReflectionData.Data == IntPtr.Zero)
                {
                    ReflectionData.Data = JobsUtility.CreateJobReflectionData(
                        typeof(JobJobHashMapVisitKeyValueProducer<T>),
                        typeof(T),
                        (ExecuteJobFunction)Execute);
                }
            }

            /// <summary> Executes the job. </summary>
            /// <param name="fullData"> The job data. </param>
            /// <param name="additionalPtr"> AdditionalPtr. </param>
            /// <param name="bufferRangePatchData"> BufferRangePatchData. </param>
            /// <param name="ranges"> The job range. </param>
            /// <param name="jobIndex"> The job index. </param>
            [SuppressMessage("ReSharper", "MemberCanBePrivate.Global", Justification = "Required by burst.")]
            internal static void Execute(
                ref JobJobHashMapVisitKeyValueProducer<T> fullData,
                IntPtr additionalPtr,
                IntPtr bufferRangePatchData,
                ref JobRanges ranges,
                int jobIndex)
            {
                while (true)
                {
                    if (!JobsUtility.GetWorkStealingRange(ref ranges, jobIndex, out var begin, out var end))
                    {
                        return;
                    }

                    var buckets = (int*)fullData.HashMap->buckets;
                    var nextPtrs = (int*)fullData.HashMap->next;
                    var keys = fullData.HashMap->keys;
                    var values = fullData.HashMap->values;

                    for (var i = begin; i < end; i++)
                    {
                        var entryIndex = buckets[i];

                        while (entryIndex != -1)
                        {
                            fullData.JobData.ExecuteNext(keys, values, entryIndex);
                            entryIndex = nextPtrs[entryIndex];
                        }
                    }
                }
            }
        }
    }
}
