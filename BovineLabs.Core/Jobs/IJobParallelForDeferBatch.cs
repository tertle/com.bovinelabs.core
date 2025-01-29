// <copyright file="IJobParallelForDeferBatch.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Jobs
{
    using System;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs;
    using Unity.Jobs.LowLevel.Unsafe;

    /// <summary>
    /// A replacement for IJobParallelForBatch when the number of work items is not known at Schedule time.
    /// IJobParallelForDeferBatch lets you calculate the number of iterations to perform in a job that must execute before the IJobParallelForDeferBatch job.
    /// When Scheduling the job's Execute(int startIndex, int count) method will be invoked on multiple worker threads in parallel to each other.
    /// Execute(int startIndex, int count) will be executed once for each index from 0 to the provided length. Each iteration must be independent from other iterations
    /// (The safety system enforces this rule for you). The indices have no guaranteed order and are executed on multiple cores in parallel.
    /// Unity automatically splits the work into chunks of no less than the provided batchSize, and schedules an appropriate number of jobs based on the number of
    /// worker threads, the length of the array and the batch size.
    /// Batch size should generally be chosen depending on the amount of work performed in the job. A simple job, for example adding a couple of float3 to each other
    /// should probably have a batch size of 32 to 128. However if the work performed is very expensive then it is best to use a small batch size, for expensive work a
    /// batch size of 1 is totally fine. IJobParallelFor performs work stealing using atomic operations. Batch sizes can be small but they are not for free.
    /// The returned JobHandle can be used to ensure that the job has completed. Or it can be passed to other jobs as a dependency, thus ensuring the jobs are executed
    /// one after another on the worker threads.
    /// </summary>
    [JobProducerType(typeof(IJobParallelForDeferBatchExtensions.IJobParallelForDeferBatchProducer<>))]
    public interface IJobParallelForDeferBatch
    {
        void Execute(int startIndex, int count);
    }

    /// <summary>
    /// Extension class for the IJobParallelForDeferBatch job type providing custom overloads for scheduling and running.
    /// </summary>
    public static class IJobParallelForDeferBatchExtensions
    {
        /// <summary>
        /// Gathers and caches reflection data for the internal job system's managed bindings. Unity is responsible for calling this method - don't call it yourself.
        /// </summary>
        /// <typeparam name="T"> </typeparam>
        /// <remarks>
        /// When the Jobs package is included in the project, Unity generates code to call EarlyJobInit at startup. This allows Burst compiled code to schedule jobs
        /// because the reflection part of initialization, which is not compatible with burst compiler constraints, has already happened in EarlyJobInit.
        /// __Note__: While the Jobs package code generator handles this automatically for all closed job types, you must register those with generic arguments (like
        /// IJobParallelForDefer&amp;lt;MyJobType&amp;lt;T&amp;gt;&amp;gt;) manually for each specialization with [[Unity.Jobs.RegisterGenericJobTypeAttribute]].
        /// </remarks>
        public static void EarlyJobInit<T>()
            where T : unmanaged, IJobParallelForDeferBatch
        {
            IJobParallelForDeferBatchProducer<T>.Initialize();
        }

        /// <summary>
        /// Schedule the job for execution on worker threads.
        /// list.Length is used as the iteration count.
        /// Note that it is required to embed the list on the job struct as well.
        /// </summary>
        /// <param name="jobData"> The job and data to schedule. </param>
        /// <param name="list"> list.Length is used as the iteration count. </param>
        /// <param name="innerloopBatchCount">
        /// Granularity in which workstealing is performed. A value of 32, means the job queue will steal 32 iterations and then perform
        /// them in an efficient inner loop.
        /// </param>
        /// <param name="dependsOn">
        /// Dependencies are used to ensure that a job executes on workerthreads after the dependency has completed execution. Making sure that
        /// two jobs reading or writing to same data do not run in parallel.
        /// </param>
        /// <returns> JobHandle The handle identifying the scheduled job. Can be used as a dependency for a later job or ensure completion on the main thread. </returns>
        /// <typeparam name="T"> Job type </typeparam>
        /// <typeparam name="U"> List element type </typeparam>
        public static unsafe JobHandle ScheduleParallel<T, U>(this T jobData, NativeList<U> list, int innerloopBatchCount, JobHandle dependsOn = default)
            where T : unmanaged, IJobParallelForDeferBatch
            where U : unmanaged
        {
            void* atomicSafetyHandlePtr = null;
            // Calculate the deferred atomic safety handle before constructing JobScheduleParameters so
            // DOTS Runtime can validate the deferred list statically similar to the reflection based
            // validation in Big Unity.
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var safety = NativeListUnsafeUtility.GetAtomicSafetyHandle(ref list);
            atomicSafetyHandlePtr = UnsafeUtility.AddressOf(ref safety);
#endif
            return ScheduleParallelBatchInternal(ref jobData, innerloopBatchCount, NativeListUnsafeUtility.GetInternalListDataPtrUnchecked(ref list),
                atomicSafetyHandlePtr, dependsOn);
        }

        /// <summary>
        /// Schedule the job for execution on a single worker thread.
        /// list.Length is used as the iteration count.
        /// Note that it is required to embed the list on the job struct as well.
        /// </summary>
        /// <param name="jobData"> The job and data to schedule. </param>
        /// <param name="list"> list.Length is used as the iteration count. </param>
        /// <param name="innerloopBatchCount">
        /// Granularity in which workstealing is performed. A value of 32, means the job queue will steal 32 iterations and then perform
        /// them in an efficient inner loop.
        /// </param>
        /// <param name="dependsOn">
        /// Dependencies are used to ensure that a job executes on workerthreads after the dependency has completed execution. Making sure that
        /// two jobs reading or writing to same data do not run in parallel.
        /// </param>
        /// <returns> JobHandle The handle identifying the scheduled job. Can be used as a dependency for a later job or ensure completion on the main thread. </returns>
        /// <typeparam name="T"> Job type. </typeparam>
        /// <typeparam name="TU"> List element type. </typeparam>
        public static unsafe JobHandle Schedule<T, TU>(this T jobData, NativeList<TU> list, int innerloopBatchCount, JobHandle dependsOn = default)
            where T : unmanaged, IJobParallelForDeferBatch
            where TU : unmanaged
        {
            // ReSharper disable once RedundantAssignment
            void* atomicSafetyHandlePtr = null;

            // Calculate the deferred atomic safety handle before constructing JobScheduleParameters so
            // DOTS Runtime can validate the deferred list statically similar to the reflection based
            // validation in Big Unity.
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var safety = NativeListUnsafeUtility.GetAtomicSafetyHandle(ref list);
            atomicSafetyHandlePtr = UnsafeUtility.AddressOf(ref safety);
#endif
            return ScheduleBatchInternal(ref jobData, innerloopBatchCount, NativeListUnsafeUtility.GetInternalListDataPtrUnchecked(ref list),
                atomicSafetyHandlePtr, dependsOn);
        }

        /// <summary>
        /// Schedule the job for execution on worker threads.
        /// list.Length is used as the iteration count.
        /// Note that it is required to embed the list on the job struct as well.
        /// </summary>
        /// <param name="jobData">
        /// The job and data to schedule. In this variant, the jobData is
        /// passed by reference, which may be necessary for unusually large job structs.
        /// </param>
        /// <param name="list"> list.Length is used as the iteration count. </param>
        /// <param name="innerloopBatchCount">
        /// Granularity in which workstealing is performed. A value of 32, means the job queue will steal 32 iterations and then perform
        /// them in an efficient inner loop.
        /// </param>
        /// <param name="dependsOn">
        /// Dependencies are used to ensure that a job executes on workerthreads after the dependency has completed execution. Making sure that
        /// two jobs reading or writing to same data do not run in parallel.
        /// </param>
        /// <returns> JobHandle The handle identifying the scheduled job. Can be used as a dependency for a later job or ensure completion on the main thread. </returns>
        /// <typeparam name="T"> Job type </typeparam>
        /// <typeparam name="U"> List element type </typeparam>
        public static unsafe JobHandle ScheduleParallelByRef<T, U>(
            this ref T jobData, NativeList<U> list, int innerloopBatchCount, JobHandle dependsOn = default)
            where T : unmanaged, IJobParallelForDeferBatch
            where U : unmanaged
        {
            void* atomicSafetyHandlePtr = null;
            // Calculate the deferred atomic safety handle before constructing JobScheduleParameters so
            // DOTS Runtime can validate the deferred list statically similar to the reflection based
            // validation in Big Unity.
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var safety = NativeListUnsafeUtility.GetAtomicSafetyHandle(ref list);
            atomicSafetyHandlePtr = UnsafeUtility.AddressOf(ref safety);
#endif
            return ScheduleParallelBatchInternal(ref jobData, innerloopBatchCount, NativeListUnsafeUtility.GetInternalListDataPtrUnchecked(ref list),
                atomicSafetyHandlePtr, dependsOn);
        }

        /// <summary>
        /// Schedule the job for execution on worker threads.
        /// forEachCount is a pointer to the number of iterations, when dependsOn has completed.
        /// This API is unsafe, it is recommended to use the NativeList based Schedule method instead.
        /// </summary>
        /// <param name="jobData"> The job and data to schedule. </param>
        /// <param name="forEachCount"> *forEachCount is used as the iteration count. </param>
        /// <param name="innerloopBatchCount">
        /// Granularity in which workstealing is performed. A value of 32, means the job queue will steal 32 iterations and then perform
        /// them in an efficient inner loop.
        /// </param>
        /// <param name="dependsOn">
        /// Dependencies are used to ensure that a job executes on workerthreads after the dependency has completed execution. Making sure that
        /// two jobs reading or writing to same data do not run in parallel.
        /// </param>
        /// <returns> JobHandle The handle identifying the scheduled job. Can be used as a dependency for a later job or ensure completion on the main thread. </returns>
        /// <typeparam name="T"> Job type </typeparam>
        /// <returns> </returns>
        public static unsafe JobHandle ScheduleParallel<T>(this T jobData, int* forEachCount, int innerloopBatchCount, JobHandle dependsOn = default)
            where T : unmanaged, IJobParallelForDeferBatch
        {
            var forEachListPtr = (byte*)forEachCount - sizeof(void*);
            return ScheduleParallelBatchInternal(ref jobData, innerloopBatchCount, forEachListPtr, null, dependsOn);
        }

        public static unsafe JobHandle ScheduleParallel<T>(
            this T jobData, NativeReference<int> forEachCount, int innerloopBatchCount, JobHandle dependsOn = default)
            where T : unmanaged, IJobParallelForDeferBatch
        {
            var forEachListPtr = (byte*)forEachCount.GetUnsafePtrWithoutChecks() - sizeof(void*);
            return ScheduleParallelBatchInternal(ref jobData, innerloopBatchCount, forEachListPtr, null, dependsOn);
        }

        /// <summary>
        /// Schedule the job for execution on worker threads.
        /// forEachCount is a pointer to the number of iterations, when dependsOn has completed.
        /// This API is unsafe, it is recommended to use the NativeList based Schedule method instead.
        /// </summary>
        /// <param name="jobData">
        /// The job and data to schedule. In this variant, the jobData is
        /// passed by reference, which may be necessary for unusually large job structs.
        /// </param>
        /// <param name="forEachCount"> *forEachCount is used as the iteration count. </param>
        /// <param name="innerloopBatchCount">
        /// Granularity in which workstealing is performed. A value of 32, means the job queue will steal 32 iterations and then perform
        /// them in an efficient inner loop.
        /// </param>
        /// <param name="dependsOn">
        /// Dependencies are used to ensure that a job executes on workerthreads after the dependency has completed execution. Making sure that
        /// two jobs reading or writing to same data do not run in parallel.
        /// </param>
        /// <returns> JobHandle The handle identifying the scheduled job. Can be used as a dependency for a later job or ensure completion on the main thread. </returns>
        /// <typeparam name="T"> </typeparam>
        /// <returns> </returns>
        public static unsafe JobHandle ScheduleParallelByRef<T>(this ref T jobData, int* forEachCount, int innerloopBatchCount, JobHandle dependsOn = default)
            where T : unmanaged, IJobParallelForDeferBatch
        {
            var forEachListPtr = (byte*)forEachCount - sizeof(void*);
            return ScheduleParallelBatchInternal(ref jobData, innerloopBatchCount, forEachListPtr, null, dependsOn);
        }

        private static unsafe JobHandle ScheduleParallelBatchInternal<T>(
            ref T jobData, int innerloopBatchCount, void* forEachListPtr, void* atomicSafetyHandlePtr, JobHandle dependsOn)
            where T : unmanaged, IJobParallelForDeferBatch
        {
            IJobParallelForDeferBatchProducer<T>.Initialize();
            var reflectionData = IJobParallelForDeferBatchProducer<T>.JobReflectionData.Data;
            CollectionHelper.CheckReflectionDataCorrect<T>(reflectionData);
            var scheduleParams = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobData), reflectionData, dependsOn, ScheduleMode.Parallel);
            return JobsUtility.ScheduleParallelForDeferArraySize(ref scheduleParams, innerloopBatchCount, forEachListPtr, atomicSafetyHandlePtr);
        }

        private static unsafe JobHandle ScheduleBatchInternal<T>(
            ref T jobData, int innerloopBatchCount, void* forEachListPtr, void* atomicSafetyHandlePtr, JobHandle dependsOn)
            where T : unmanaged, IJobParallelForDeferBatch
        {
            IJobParallelForDeferBatchProducer<T>.Initialize();
            var reflectionData = IJobParallelForDeferBatchProducer<T>.JobReflectionData.Data;
            CollectionHelper.CheckReflectionDataCorrect<T>(reflectionData);
            var scheduleParams = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobData), reflectionData, dependsOn, ScheduleMode.Single);
            return JobsUtility.ScheduleParallelForDeferArraySize(ref scheduleParams, innerloopBatchCount, forEachListPtr, atomicSafetyHandlePtr);
        }

        internal struct IJobParallelForDeferBatchProducer<T>
            where T : unmanaged, IJobParallelForDeferBatch
        {
            internal static readonly SharedStatic<IntPtr> JobReflectionData = SharedStatic<IntPtr>.GetOrCreate<IJobParallelForDeferBatchProducer<T>>();

            [BurstDiscard]
            internal static void Initialize()
            {
                if (JobReflectionData.Data == IntPtr.Zero)
                {
                    JobReflectionData.Data = JobsUtility.CreateJobReflectionData(typeof(T), (ExecuteJobFunction)Execute);
                }
            }

            public delegate void ExecuteJobFunction(ref T jobData, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex);

            public static unsafe void Execute(ref T jobData, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex)
            {
                while (true)
                {
                    if (!JobsUtility.GetWorkStealingRange(ref ranges, jobIndex, out var begin, out var end))
                    {
                        break;
                    }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    JobsUtility.PatchBufferMinMaxRanges(bufferRangePatchData, UnsafeUtility.AddressOf(ref jobData), begin, end - begin);
#endif

                    jobData.Execute(begin, end - begin);
                }
            }
        }
    }
}
