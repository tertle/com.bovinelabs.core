// <copyright file="IJobNativeHashMapVisitKeyValue.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Basics.Jobs
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs;
    using Unity.Jobs.LowLevel.Unsafe;

    /// <summary> Job that visits each key value pair in a <see cref="NativeHashMap{TKey,TValue}" />. </summary>
    /// <typeparam name="TKey">The key type of the hash map.</typeparam>
    /// <typeparam name="TValue">The value type of the hash map.</typeparam>
    [JobProducerType(typeof(JobNativeHashMapVisitKeyValue.NativeHashMapVisitKeyValueJobStruct<,,>))]
    [SuppressMessage("ReSharper", "TypeParameterCanBeVariant", Justification = "Strict requirements for compiler")]
    public interface IJobNativeHashMapVisitKeyValue<TKey, TValue>
        where TKey : struct, IEquatable<TKey>
        where TValue : struct
    {
        /// <summary> Executes the next key value pair in the <see cref="NativeHashMap{TKey, TValue}" />. </summary>
        /// <param name="key"> The key. </param>
        /// <param name="value"> The value. </param>
        void ExecuteNext(TKey key, TValue value);
    }

    /// <summary> Extension methods for <see cref="IJobNativeHashMapVisitKeyValue{TKey,TValue}" />. </summary>
    public static class JobNativeHashMapVisitKeyValue
    {
        /// <summary> Schedule a <see cref="IJobNativeHashMapVisitKeyValue{TKey,TValue}" /> job. </summary>
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
            NativeHashMap<TKey, TValue> hashMap,
            int minIndicesPerJobCount,
            JobHandle dependsOn = default)
            where TJob : struct, IJobNativeHashMapVisitKeyValue<TKey, TValue>
            where TKey : struct, IEquatable<TKey>
            where TValue : struct
        {
            var data = hashMap.GetBucketData();

            var fullData = new NativeHashMapVisitKeyValueJobStruct<TJob, TKey, TValue>
            {
                HashMap = hashMap,
                JobData = jobData,
            };

            var scheduleParams = new JobsUtility.JobScheduleParameters(
                UnsafeUtility.AddressOf(ref fullData),
                NativeHashMapVisitKeyValueJobStruct<TJob, TKey, TValue>.Initialize(),
                dependsOn,
                ScheduleMode.Parallel);

            return JobsUtility.ScheduleParallelFor(ref scheduleParams, data.bucketCapacityMask + 1, minIndicesPerJobCount);
        }

        /// <summary> The job execution struct. </summary>
        /// <typeparam name="TJob"> The type of the job. </typeparam>
        /// <typeparam name="TKey"> The type of the key in the hash map. </typeparam>
        /// <typeparam name="TValue"> The type of the value in the hash map. </typeparam>
        internal struct NativeHashMapVisitKeyValueJobStruct<TJob, TKey, TValue>
            where TJob : struct, IJobNativeHashMapVisitKeyValue<TKey, TValue>
            where TKey : struct, IEquatable<TKey>
            where TValue : struct
        {
            /// <summary> The <see cref="NativeHashMap{TKey,TValue}" />. </summary>
            [ReadOnly]
            public NativeHashMap<TKey, TValue> HashMap;

            /// <summary> The job. </summary>
            internal TJob JobData;

            // ReSharper disable once StaticMemberInGenericType
            private static IntPtr jobReflectionData;

            private delegate void ExecuteJobFunction(
                ref NativeHashMapVisitKeyValueJobStruct<TJob, TKey, TValue> fullData,
                IntPtr additionalPtr,
                IntPtr bufferRangePatchData,
                ref JobRanges ranges,
                int jobIndex);

            /// <summary> Initializes the job. </summary>
            /// <returns> The job pointer. </returns>
            internal static IntPtr Initialize()
            {
                if (jobReflectionData == IntPtr.Zero)
                {
                    jobReflectionData = JobsUtility.CreateJobReflectionData(
                        typeof(NativeHashMapVisitKeyValueJobStruct<TJob, TKey, TValue>),
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
                ref NativeHashMapVisitKeyValueJobStruct<TJob, TKey, TValue> fullData,
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

                    var data = fullData.HashMap.GetBucketData();

                    var buckets = (int*)data.buckets;
                    var bucketNext = (int*)data.next;
                    var keys = data.keys;
                    var values = data.values;

                    for (var i = begin; i < end; i++)
                    {
                        var entryIndex = buckets[i];

                        while (entryIndex != -1)
                        {
                            var key = UnsafeUtility.ReadArrayElement<TKey>(keys, entryIndex);
                            var value = UnsafeUtility.ReadArrayElement<TValue>(values, entryIndex);
                            fullData.JobData.ExecuteNext(key, value);

                            // TODO is this needed for a non multi map?
                            entryIndex = bucketNext[entryIndex];
                        }
                    }
                }
            }
        }
    }
}