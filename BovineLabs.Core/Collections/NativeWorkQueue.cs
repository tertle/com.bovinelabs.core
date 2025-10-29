// <copyright file="NativeWorkQueue.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Collections
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using Unity.Burst;
    using Unity.Burst.CompilerServices;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs;
    using Unity.Mathematics;

    [NativeContainer]
    public unsafe struct NativeWorkQueue<T>
        where T : unmanaged
    {
        [NativeDisableUnsafePtrRestriction]
        private readonly T* queue;

        [NativeDisableUnsafePtrRestriction]
        private readonly int* queueWriteHead;

        [NativeDisableUnsafePtrRestriction]
        private readonly int* queueReadHead;

        [NativeDisableUnsafePtrRestriction]
        private readonly int* currentRef;

        private readonly AllocatorManager.AllocatorHandle allocator;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private AtomicSafetyHandle m_Safety;
        private static readonly SharedStatic<int> s_staticSafetyId = SharedStatic<int>.GetOrCreate<NativeWorkQueue<T>>();
#endif

        public NativeWorkQueue(int maxQueueSize, AllocatorManager.AllocatorHandle allocator)
        {
            this.allocator = allocator.Handle;
            this.queue = (T*)AllocatorManager.Allocate(allocator, UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(), maxQueueSize);
            this.queueWriteHead = (int*)AllocatorManager.Allocate(allocator, UnsafeUtility.SizeOf<int>(), UnsafeUtility.AlignOf<int>());

            this.queueReadHead = (int*)AllocatorManager.Allocate(allocator, UnsafeUtility.SizeOf<int>(), UnsafeUtility.AlignOf<int>());
            this.currentRef = (int*)AllocatorManager.Allocate(allocator, UnsafeUtility.SizeOf<int>(), UnsafeUtility.AlignOf<int>());

            this.Capacity = maxQueueSize;

            *this.queueWriteHead = 0;
            *this.queueReadHead = 0;
            *this.currentRef = 0;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CollectionHelper.CheckAllocator(allocator.Handle);

            this.m_Safety = CollectionHelper.CreateSafetyHandle(allocator.Handle);
            CollectionHelper.InitNativeContainer<T>(this.m_Safety);
            CollectionHelper.SetStaticSafetyId<NativeWorkQueue<T>>(ref this.m_Safety, ref s_staticSafetyId.Data);
            AtomicSafetyHandle.SetBumpSecondaryVersionOnScheduleWrite(this.m_Safety, true);
#endif
        }

        public int Length
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(this.m_Safety);
#endif
                // Our queue system can push the length of the list above it's capacity so it's actual length is whatever is smaller
                return math.min(this.Capacity, *this.queueWriteHead);
            }
        }

        public int Capacity { get; }

        public bool HasCapacity => this.Length < this.Capacity;

        public void Dispose()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CollectionHelper.DisposeSafetyHandle(ref this.m_Safety);
#endif

            AllocatorManager.Free(this.allocator, this.queue, this.Capacity);
            AllocatorManager.Free(this.allocator, this.queueWriteHead);
            AllocatorManager.Free(this.allocator, this.queueReadHead);
            AllocatorManager.Free(this.allocator, this.currentRef);
        }

        public void Update()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(this.m_Safety);
#endif

            *this.queueWriteHead = 0; // math.min(*this.queueWriteHead, this.Capacity);
            *this.queueReadHead = 0; // math.min(*this.queueReadHead, *this.queueWriteHead);
        }

        public JobHandle Update(JobHandle handle)
        {
            return new UpdateNativeWorkQueueJob
            {
                QueueReadHead = this.queueReadHead,
                QueueWriteHead = this.queueWriteHead,
            }.Schedule(handle);
        }

        /// <summary> Try add some work to the queue. </summary>
        /// <param name="ptr"> The work slot. </param>
        /// <returns> 0 if the queue is full, otherwise a unique ID for the work. </returns>
        public int TryAdd(out T* ptr)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(this.m_Safety);
#endif

            if (this.Capacity < *this.queueWriteHead + 1)
            {
                // we've gone past end of list, don't write we'll requeue this next frame
                ptr = null;

                return 0;
            }

            *this.queueWriteHead += 1;

            int queueRef;
            do
            {
                queueRef = ++*this.currentRef;
            }
            while (Hint.Unlikely(queueRef == 0));

            ptr = (this.queue + *this.queueWriteHead) - 1;
            return queueRef;
        }

        public T* Add(out int queueRef)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(this.m_Safety);
#endif

            CheckSufficientCapacity(this.Capacity, *this.queueWriteHead + 1);

            *this.queueWriteHead += 1;

            do
            {
                queueRef = ++*this.currentRef;
            }
            while (Hint.Unlikely(queueRef == 0));

            return (this.queue + *this.queueWriteHead) - 1;
        }

        public ParallelReader AsParallelReader()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            return new ParallelReader(this, ref this.m_Safety);
#else
            return new ParallelReader(this);
#endif
        }

        public ParallelWriter AsParallelWriter()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            return new ParallelWriter(this, ref this.m_Safety);
#else
            return new ParallelWriter(this);
#endif
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        public static void CheckSufficientCapacity(int capacity, int length)
        {
            if (capacity < length)
            {
                throw new Exception($"Length {length} exceeds Capacity {capacity}");
            }
        }

        [NativeContainer]
        [NativeContainerIsAtomicWriteOnly]
        public struct ParallelReader
        {
            [NativeDisableUnsafePtrRestriction]
            private readonly T* queue;

            [NativeDisableUnsafePtrRestriction]
            private readonly int* queueReadHead;

            [NativeDisableUnsafePtrRestriction]
            private readonly int* queueWriteHead;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            internal readonly AtomicSafetyHandle m_Safety;
            internal static readonly SharedStatic<int> s_staticSafetyId = SharedStatic<int>.GetOrCreate<ParallelWriter>();

            internal ParallelReader(NativeWorkQueue<T> workQueue, ref AtomicSafetyHandle safety)
            {
                this.queue = workQueue.queue;
                this.queueReadHead = workQueue.queueReadHead;
                this.queueWriteHead = workQueue.queueWriteHead;
                this.Capacity = workQueue.Capacity;
                this.m_Safety = safety;
                CollectionHelper.SetStaticSafetyId<ParallelWriter>(ref this.m_Safety, ref s_staticSafetyId.Data);
            }
#else
            internal ParallelReader(NativeWorkQueue<T> workQueue)
            {
                this.queue = workQueue.queue;
                this.queueReadHead = workQueue.queueReadHead;
                this.queueWriteHead = workQueue.queueWriteHead;
                this.Capacity = workQueue.Capacity;
            }
#endif
            public int Capacity { get; }

            public int Length
            {
                get
                {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    AtomicSafetyHandle.CheckWriteAndThrow(this.m_Safety);
#endif
                    // Our queue system can push the length of the list above it's capacity so it's actual length is whatever is smaller
                    return math.min(this.Capacity, *this.queueWriteHead);
                }
            }

            public bool TryGetNext(out T* value)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndThrow(this.m_Safety);
#endif
                var idx = Interlocked.Increment(ref *this.queueReadHead) - 1;

                if (idx >= this.Length)
                {
                    value = null;
                    return false;
                }

                value = this.queue + idx;
                return true;
            }
        }

        [NativeContainer]
        [NativeContainerIsAtomicWriteOnly]
        public struct ParallelWriter
        {
            [NativeDisableUnsafePtrRestriction]
            private readonly T* queue;

            [NativeDisableUnsafePtrRestriction]
            private readonly int* queueWriteHead;

            [NativeDisableUnsafePtrRestriction]
            private readonly int* currentRef;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            internal readonly AtomicSafetyHandle m_Safety;
            internal static readonly SharedStatic<int> s_staticSafetyId = SharedStatic<int>.GetOrCreate<ParallelWriter>();

            internal ParallelWriter(NativeWorkQueue<T> workQueue, ref AtomicSafetyHandle safety)
            {
                this.queue = workQueue.queue;
                this.queueWriteHead = workQueue.queueWriteHead;
                this.currentRef = workQueue.currentRef;
                this.Capacity = workQueue.Capacity;
                this.m_Safety = safety;
                CollectionHelper.SetStaticSafetyId<ParallelWriter>(ref this.m_Safety, ref s_staticSafetyId.Data);
            }
#else
            internal ParallelWriter(NativeWorkQueue<T> workQueue)
            {
                this.queue = workQueue.queue;
                this.queueWriteHead = workQueue.queueWriteHead;
                this.currentRef = workQueue.currentRef;
                this.Capacity = workQueue.Capacity;
            }
#endif

            public int Capacity { get; }

            /// <summary> Try add some work to the queue. </summary>
            /// <param name="ptr"> The work slot. </param>
            /// <returns> 0 if the queue is full, otherwise a unique ID for the work. </returns>
            public int TryAdd(out T* ptr)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndThrow(this.m_Safety);
#endif

                var idx = Interlocked.Increment(ref *this.queueWriteHead) - 1;

                if (idx >= this.Capacity)
                {
                    // we've gone past end of list, don't write we'll requeue this next frame
                    ptr = null;

                    return 0;
                }

                int queueRef;
                do
                {
                    queueRef = Interlocked.Increment(ref *this.currentRef);
                }
                while (Hint.Unlikely(queueRef == 0));

                ptr = this.queue + idx;
                return queueRef;
            }
        }
    }

    [BurstCompile]
    internal unsafe struct UpdateNativeWorkQueueJob : IJob
    {
        [NativeDisableUnsafePtrRestriction]
        public int* QueueWriteHead;

        [NativeDisableUnsafePtrRestriction]
        public int* QueueReadHead;

        public void Execute()
        {
            // TODO safety?
            *this.QueueWriteHead = 0;
            *this.QueueReadHead = 0;
        }
    }
}
