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

    [JobProducerType(typeof(JobForThread.JobThreadStruct<>))]
    public interface IJobForThread
    {
        void Execute(int index);
    }

    public static unsafe class JobForThread
    {
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
