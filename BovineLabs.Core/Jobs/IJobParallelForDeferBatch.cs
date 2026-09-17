namespace BovineLabs.Core.Jobs
{
    using System;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs;
    using Unity.Jobs.LowLevel.Unsafe;

    /// <summary>
    /// The iteration count is resolved after dependencies complete; batches must be independent and may execute in any order.
    /// </summary>
    [JobProducerType(typeof(IJobParallelForDeferBatchExtensions.IJobParallelForDeferBatchProducer<>))]
    public interface IJobParallelForDeferBatch
    {
        void Execute(int startIndex, int count);
    }

    public static class IJobParallelForDeferBatchExtensions
    {
        /// <summary>
        /// Unity initializes closed job types automatically; register each generic specialization with RegisterGenericJobTypeAttribute.
        /// </summary>
        public static void EarlyJobInit<T>()
            where T : unmanaged, IJobParallelForDeferBatch
        {
            IJobParallelForDeferBatchProducer<T>.Initialize();
        }

        /// <summary>
        /// The list must also be embedded in the job struct; its length is resolved after dependencies complete.
        /// </summary>
        public static unsafe JobHandle ScheduleParallel<T, U>(this T jobData, NativeList<U> list, int innerloopBatchCount, JobHandle dependsOn = default)
            where T : unmanaged, IJobParallelForDeferBatch
            where U : unmanaged
        {
            // Calculate the deferred atomic safety handle before constructing JobScheduleParameters so
            // DOTS Runtime can validate the deferred list statically similar to the reflection based
            // validation in Big Unity.
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var safety = NativeListUnsafeUtility.GetAtomicSafetyHandle(ref list);
            void* atomicSafetyHandlePtr = UnsafeUtility.AddressOf(ref safety);
#else
            void* atomicSafetyHandlePtr = null;
#endif

            return ScheduleParallelBatchInternal(ref jobData, innerloopBatchCount, list.GetUnsafeList(), atomicSafetyHandlePtr, dependsOn);
        }

        /// <summary>
        /// The list must also be embedded in the job struct; its length is resolved after dependencies complete.
        /// </summary>
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
            return ScheduleBatchInternal(ref jobData, innerloopBatchCount, list.GetUnsafeList(), atomicSafetyHandlePtr, dependsOn);
        }

        /// <summary>
        /// The list must also be embedded in the job struct; its length is resolved after dependencies complete.
        /// </summary>
        public static unsafe JobHandle ScheduleParallelByRef<T, U>(
            this ref T jobData, NativeList<U> list, int innerloopBatchCount, JobHandle dependsOn = default)
            where T : unmanaged, IJobParallelForDeferBatch
            where U : unmanaged
        {
            // Calculate the deferred atomic safety handle before constructing JobScheduleParameters so
            // DOTS Runtime can validate the deferred list statically similar to the reflection based
            // validation in Big Unity.
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var safety = NativeListUnsafeUtility.GetAtomicSafetyHandle(ref list);
            void* atomicSafetyHandlePtr = UnsafeUtility.AddressOf(ref safety);
#else
            void* atomicSafetyHandlePtr = null;
#endif
            return ScheduleParallelBatchInternal(ref jobData, innerloopBatchCount, list.GetUnsafeList(), atomicSafetyHandlePtr, dependsOn);
        }

        /// <summary>
        /// The count pointer is read after dependsOn completes; prefer the NativeList overload for safety.
        /// </summary>
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
        /// The count pointer is read after dependsOn completes; prefer the NativeList overload for safety.
        /// </summary>
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
