// <copyright file="IJobNativeMultiHashMapVisitKeyValue.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Jobs
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs;
    using Unity.Jobs.LowLevel.Unsafe;

    /// <summary> Job that visits each key value pair in a <see cref="NativeMultiHashMap{TKey,TValue}" />. </summary>
    /// <typeparam name="TKey">The key type of the hash map.</typeparam>
    /// <typeparam name="TValue">The value type of the hash map.</typeparam>
    [JobProducerType(typeof(JobNativeMultiHashMapVisitKeyValue.JobNativeMultiHashMapVisitKeyValueProducer<,,>))]
    [SuppressMessage("ReSharper", "TypeParameterCanBeVariant", Justification = "Strict requirements for compiler")]
    public interface IJobNativeMultiHashMapVisitKeyValue<TKey, TValue>
        where TKey : struct, IEquatable<TKey>
        where TValue : struct
    {
        /// <summary> Executes the next key value pair in the <see cref="NativeMultiHashMap{TKey, TValue}" />. </summary>
        /// <param name="key"> The key. </param>
        /// <param name="value"> The value. </param>
        void ExecuteNext(TKey key, TValue value);
    }

    /// <summary> Extension methods for <see cref="IJobNativeMultiHashMapVisitKeyValue{TKey,TValue}" />. </summary>
    public static class JobNativeMultiHashMapVisitKeyValue
    {
        /// <summary> Schedule a <see cref="IJobNativeMultiHashMapVisitKeyValue{TKey,TValue}" /> job. </summary>
        /// <param name="jobData"> The job. </param>
        /// <param name="hashMap"> The hash map. </param>
        /// <param name="minIndicesPerJobCount"> Min indices per job count. </param>
        /// <param name="dependsOn"> The job handle dependency. </param>
        /// <typeparam name="TJob"> The type of the job. </typeparam>
        /// <typeparam name="TKey"> The type of the key in the hash map. </typeparam>
        /// <typeparam name="TValue"> The type of the value in the hash map. </typeparam>
        /// <returns> The handle to job. </returns>
        public static unsafe JobHandle ScheduleParallel<TJob, TKey, TValue>(
            this TJob jobData,
            NativeMultiHashMap<TKey, TValue> hashMap,
            int minIndicesPerJobCount,
            JobHandle dependsOn = default)
            where TJob : unmanaged, IJobNativeMultiHashMapVisitKeyValue<TKey, TValue>
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            var jobProducer = new JobNativeMultiHashMapVisitKeyValueProducer<TJob, TKey, TValue>
            {
                HashMap = hashMap,
                JobData = jobData,
            };

            var scheduleParams = new JobsUtility.JobScheduleParameters(
                UnsafeUtility.AddressOf(ref jobProducer),
                JobNativeMultiHashMapVisitKeyValueProducer<TJob, TKey, TValue>.Initialize(),
                dependsOn,
                ScheduleMode.Parallel);

            return JobsUtility.ScheduleParallelFor(ref scheduleParams, hashMap.GetUnsafeBucketData().bucketCapacityMask + 1, minIndicesPerJobCount);
        }

        /// <summary> Schedule a <see cref="IJobNativeMultiHashMapVisitKeyValue{TKey,TValue}" /> job. </summary>
        /// <param name="jobData"> The job. </param>
        /// <param name="hashMap"> The hash map. </param>
        /// <param name="dependsOn"> The job handle dependency. </param>
        /// <typeparam name="TJob"> The type of the job. </typeparam>
        /// <typeparam name="TKey"> The type of the key in the hash map. </typeparam>
        /// <typeparam name="TValue"> The type of the value in the hash map. </typeparam>
        /// <returns> The handle to job. </returns>
        public static unsafe JobHandle Schedule<TJob, TKey, TValue>(
            this TJob jobData,
            NativeMultiHashMap<TKey, TValue> hashMap,
            JobHandle dependsOn = default)
            where TJob : unmanaged, IJobNativeMultiHashMapVisitKeyValue<TKey, TValue>
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            var jobProducer = new JobNativeMultiHashMapVisitKeyValueProducer<TJob, TKey, TValue>
            {
                HashMap = hashMap,
                JobData = jobData,
            };

            var scheduleParams = new JobsUtility.JobScheduleParameters(
                UnsafeUtility.AddressOf(ref jobProducer),
                JobNativeMultiHashMapVisitKeyValueProducer<TJob, TKey, TValue>.Initialize(),
                dependsOn,
                ScheduleMode.Single);

            var length = hashMap.GetUnsafeBucketData().bucketCapacityMask + 1;
            return JobsUtility.ScheduleParallelFor(ref scheduleParams, length, length);
        }

        /// <summary> The job execution struct. </summary>
        /// <typeparam name="TJob"> The type of the job. </typeparam>
        /// <typeparam name="TKey"> The type of the key in the hash map. </typeparam>
        /// <typeparam name="TValue"> The type of the value in the hash map. </typeparam>
        internal struct JobNativeMultiHashMapVisitKeyValueProducer<TJob, TKey, TValue>
            where TJob : unmanaged, IJobNativeMultiHashMapVisitKeyValue<TKey, TValue>
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            /// <summary> The <see cref="NativeMultiHashMap{TKey,TValue}" />. </summary>
            [ReadOnly]
            public NativeMultiHashMap<TKey, TValue> HashMap;

            /// <summary> The job. </summary>
            internal TJob JobData;

            // ReSharper disable once StaticMemberInGenericType
            private static IntPtr jobReflectionData;

            private delegate void ExecuteJobFunction(
                ref JobNativeMultiHashMapVisitKeyValueProducer<TJob, TKey, TValue> producer,
                IntPtr additionalPtr,
                IntPtr bufferRangePatchData,
                ref JobRanges ranges,
                int jobIndex);

            internal static IntPtr Initialize()
            {
                if (jobReflectionData == IntPtr.Zero)
                {
                    jobReflectionData = JobsUtility.CreateJobReflectionData(
                        typeof(JobNativeMultiHashMapVisitKeyValueProducer<TJob, TKey, TValue>),
                        typeof(TJob),
                        (ExecuteJobFunction)Execute);
                }

                return jobReflectionData;
            }

            /// <summary> Executes the job. </summary>
            /// <param name="fullData"> The job data. </param>
            /// <param name="additionalPtr"> AdditionalPtr. </param>
            /// <param name="bufferRangePatchData"> BufferRangePatchData. </param>
            /// <param name="ranges"> The job range. </param>
            /// <param name="jobIndex"> The job index. </param>
            [SuppressMessage("ReSharper", "MemberCanBePrivate.Global", Justification = "Required by burst.")]
            internal static unsafe void Execute(
                ref JobNativeMultiHashMapVisitKeyValueProducer<TJob, TKey, TValue> fullData,
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

                    var bucketData = fullData.HashMap.GetUnsafeBucketData();

                    var buckets = (int*)bucketData.buckets;
                    var nextPtrs = (int*)bucketData.next;
                    var keys = bucketData.keys;
                    var values = bucketData.values;

                    for (int i = begin; i < end; i++)
                    {
                        int entryIndex = buckets[i];

                        while (entryIndex != -1)
                        {
                            var key = UnsafeUtility.ReadArrayElement<TKey>(keys, entryIndex);
                            var value = UnsafeUtility.ReadArrayElement<TValue>(values, entryIndex);

                            fullData.JobData.ExecuteNext(key, value);

                            entryIndex = nextPtrs[entryIndex];
                        }
                    }
                }
            }
        }
    }
}
