// <copyright file="IJobForThread.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Jobs
{
    using System;
    using JetBrains.Annotations;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs;
    using Unity.Jobs.LowLevel.Unsafe;
    using Unity.Mathematics;

    /// <summary>
    /// A lightweight job type that divides a fixed range of work across a known number of threads, similar to
    /// <see cref="Unity.Jobs.IJobFor" />.
    /// Each worker receives a contiguous chunk of indices defined by the thread count you pass to
    /// <see cref="JobForThread.ScheduleParallel{T}" />.
    /// </summary>
    [JobProducerType(typeof(JobForThread.JobThreadStruct<>))]
    public interface IJobForThread
    {
        /// <summary> Performs work against a specific iteration index. </summary>
        /// <param name="index">The index of the for loop to perform work from.</param>
        void Execute(int index);
    }

    /// <summary>
    /// Scheduling helpers for <see cref="IJobForThread" /> implementations.
    /// </summary>
    public static unsafe class JobForThread
    {
        /// <summary>
        /// Schedules the job across a fixed number of worker threads where each thread processes a contiguous slice of the given length.
        /// </summary>
        /// <param name="jobData"> The job instance to schedule. </param>
        /// <param name="arrayLength"> The total number of iterations to execute. </param>
        /// <param name="threadCount"> The number of worker threads to divide the iterations across. </param>
        /// <param name="dependency"> A handle identifying jobs that must complete before this job begins. </param>
        /// <typeparam name="T"> The specific <see cref="IJobForThread" /> implementation type. </typeparam>
        /// <returns> A handle representing the scheduled job. </returns>
        public static JobHandle ScheduleParallel<T>(this T jobData, int arrayLength, int threadCount, JobHandle dependency)
            where T : struct, IJobForThread
        {
            // Need to always check for 0 count in case Use Job Threads option in preferences - bad things happen
            threadCount = math.max(1, threadCount);

            var jobProducer = new JobThreadStruct<T>
            {
                JobData = jobData,
                Length = arrayLength,
                Threads = threadCount,
            };

            var reflectionData = GetReflectionData<T>();

            var scheduleParams = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobProducer),
                reflectionData, dependency, ScheduleMode.Parallel);

            return JobsUtility.ScheduleParallelFor(ref scheduleParams, threadCount, 1);
        }

        [UsedImplicitly]
        public static void EarlyJobInit<T>()
            where T : struct, IJobForThread
        {
            JobThreadStruct<T>.Initialize();
        }

        private static IntPtr GetReflectionData<T>()
            where T : struct, IJobForThread
        {
            JobThreadStruct<T>.Initialize();
            var reflectionData = JobThreadStruct<T>.ReflectionData.Data;
            CollectionHelper.CheckReflectionDataCorrect<T>(reflectionData);
            return reflectionData;
        }

        /// <summary> The job execution struct. </summary>
        /// <typeparam name="T"> The type of the job. </typeparam>
        internal struct JobThreadStruct<T>
            where T : struct, IJobForThread
        {
            internal static readonly SharedStatic<IntPtr> ReflectionData = SharedStatic<IntPtr>.GetOrCreate<JobThreadStruct<T>>();

            internal T JobData;
            internal int Length;
            internal int Threads;

            private delegate void ExecuteJobFunction(
                ref JobThreadStruct<T> data, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex);

            [BurstDiscard]
            internal static void Initialize()
            {
                if (ReflectionData.Data == IntPtr.Zero)
                {
                    ReflectionData.Data = JobsUtility.CreateJobReflectionData(typeof(JobThreadStruct<T>), typeof(T), (ExecuteJobFunction)Execute);
                }
            }

            private static void Execute(ref JobThreadStruct<T> fullData, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex)
            {
                while (true)
                {
                    if (!JobsUtility.GetWorkStealingRange(ref ranges, jobIndex, out var beginWorkerIndex, out var endWorkerIndex))
                    {
                        return;
                    }

                    var perThread = fullData.Length / fullData.Threads;

                    var beginIndex = beginWorkerIndex * perThread;
                    var endIndex = endWorkerIndex * perThread;
                    if (endWorkerIndex == fullData.Threads)
                    {
                        endIndex += fullData.Length % fullData.Threads;
                    }

                    JobsUtility.PatchBufferMinMaxRanges(bufferRangePatchData, UnsafeUtility.AddressOf(ref fullData), beginIndex, endIndex - beginIndex);
                    for (var index = beginIndex; index < endIndex; ++index)
                    {
                        fullData.JobData.Execute(index);
                    }
                }
            }
        }
    }
}
