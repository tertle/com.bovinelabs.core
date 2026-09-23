namespace BovineLabs.Core.Collections
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;
    using BovineLabs.Core.Internal;
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
        private readonly T* _queue;

        [NativeDisableUnsafePtrRestriction]
        private readonly int* _queueWriteHead;

        [NativeDisableUnsafePtrRestriction]
        private readonly int* _queueReadHead;

        [NativeDisableUnsafePtrRestriction]
        private readonly int* _currentRef;

        private readonly AllocatorManager.AllocatorHandle _allocator;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Preserve Unity safety-handle field names.")]
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Preserve Unity safety-handle field names.")]
        private AtomicSafetyHandle m_Safety;
        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Preserve the established static safety ID name.")]
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Preserve the established static safety ID name.")]
        private static readonly SharedStatic<int> s_staticSafetyId = SharedStatic<int>.GetOrCreate<NativeWorkQueue<T>>();
#endif

        public NativeWorkQueue(int maxQueueSize, AllocatorManager.AllocatorHandle allocator)
        {
            _allocator = allocator.Handle;
            _queue = (T*)AllocatorManager.Allocate(allocator, UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(), maxQueueSize);
            _queueWriteHead = (int*)AllocatorManager.Allocate(allocator, UnsafeUtility.SizeOf<int>(), UnsafeUtility.AlignOf<int>());

            _queueReadHead = (int*)AllocatorManager.Allocate(allocator, UnsafeUtility.SizeOf<int>(), UnsafeUtility.AlignOf<int>());
            _currentRef = (int*)AllocatorManager.Allocate(allocator, UnsafeUtility.SizeOf<int>(), UnsafeUtility.AlignOf<int>());

            Capacity = maxQueueSize;

            *_queueWriteHead = 0;
            *_queueReadHead = 0;
            *_currentRef = 0;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CollectionChecks.CheckAllocator(allocator.Handle);

            m_Safety = CollectionHelper.CreateSafetyHandle(allocator.Handle);
            CollectionChecks.InitNativeContainer<T>(m_Safety);
            CollectionHelper.SetStaticSafetyId<NativeWorkQueue<T>>(ref m_Safety, ref s_staticSafetyId.Data);
            AtomicSafetyHandle.SetBumpSecondaryVersionOnScheduleWrite(m_Safety, true);
#endif
        }

        public int Length
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                // Our queue system can push the length of the list above it's capacity so it's actual length is whatever is smaller
                return math.min(Capacity, *_queueWriteHead);
            }
        }

        public int Capacity { get; }

        public bool HasCapacity => Length < Capacity;

        public void Dispose()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CollectionHelper.DisposeSafetyHandle(ref m_Safety);
#endif

            AllocatorManager.Free(_allocator, _queue, Capacity);
            AllocatorManager.Free(_allocator, _queueWriteHead);
            AllocatorManager.Free(_allocator, _queueReadHead);
            AllocatorManager.Free(_allocator, _currentRef);
        }

        public void Update()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif

            *_queueWriteHead = 0;
            *_queueReadHead = 0;
        }

        public JobHandle Update(JobHandle handle)
        {
            return new UpdateNativeWorkQueueJob
            {
                QueueReadHead = _queueReadHead,
                QueueWriteHead = _queueWriteHead,
            }.Schedule(handle);
        }

        public int TryAdd(out T* ptr)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif

            if (Capacity < *_queueWriteHead + 1)
            {
                // we've gone past end of list, don't write we'll requeue this next frame
                ptr = null;

                return 0;
            }

            *_queueWriteHead += 1;

            int queueRef;
            do
            {
                queueRef = ++*_currentRef;
            }
            while (Hint.Unlikely(queueRef == 0));

            ptr = (_queue + *_queueWriteHead) - 1;
            return queueRef;
        }

        public T* Add(out int queueRef)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif

            CheckSufficientCapacity(Capacity, *_queueWriteHead + 1);

            *_queueWriteHead += 1;

            do
            {
                queueRef = ++*_currentRef;
            }
            while (Hint.Unlikely(queueRef == 0));

            return (_queue + *_queueWriteHead) - 1;
        }

        public ParallelReader AsParallelReader()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            return new ParallelReader(this, ref m_Safety);
#else
            return new ParallelReader(this);
#endif
        }

        public ParallelWriter AsParallelWriter()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            return new ParallelWriter(this, ref m_Safety);
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
            private readonly T* _queue;

            [NativeDisableUnsafePtrRestriction]
            private readonly int* _queueReadHead;

            [NativeDisableUnsafePtrRestriction]
            private readonly int* _queueWriteHead;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Preserve Unity safety-handle field names.")]
            [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Preserve Unity safety-handle field names.")]
            internal readonly AtomicSafetyHandle m_Safety;
            [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Preserve the established static safety ID name.")]
            [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Preserve the established static safety ID name.")]
            internal static readonly SharedStatic<int> s_staticSafetyId = SharedStatic<int>.GetOrCreate<ParallelWriter>();

            internal ParallelReader(NativeWorkQueue<T> workQueue, ref AtomicSafetyHandle safety)
            {
                _queue = workQueue._queue;
                _queueReadHead = workQueue._queueReadHead;
                _queueWriteHead = workQueue._queueWriteHead;
                Capacity = workQueue.Capacity;
                m_Safety = safety;
                CollectionHelper.SetStaticSafetyId<ParallelWriter>(ref m_Safety, ref s_staticSafetyId.Data);
            }
#else
            internal ParallelReader(NativeWorkQueue<T> workQueue)
            {
                _queue = workQueue._queue;
                _queueReadHead = workQueue._queueReadHead;
                _queueWriteHead = workQueue._queueWriteHead;
                Capacity = workQueue.Capacity;
            }
#endif
            public int Capacity { get; }

            public int Length
            {
                get
                {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
                    // Our queue system can push the length of the list above it's capacity so it's actual length is whatever is smaller
                    return math.min(Capacity, *_queueWriteHead);
                }
            }

            public bool TryGetNext(out T* value)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
                var idx = Interlocked.Increment(ref *_queueReadHead) - 1;

                if (idx >= Length)
                {
                    value = null;
                    return false;
                }

                value = _queue + idx;
                return true;
            }
        }

        [NativeContainer]
        [NativeContainerIsAtomicWriteOnly]
        public struct ParallelWriter
        {
            [NativeDisableUnsafePtrRestriction]
            private readonly T* _queue;

            [NativeDisableUnsafePtrRestriction]
            private readonly int* _queueWriteHead;

            [NativeDisableUnsafePtrRestriction]
            private readonly int* _currentRef;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Preserve Unity safety-handle field names.")]
            [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Preserve Unity safety-handle field names.")]
            internal readonly AtomicSafetyHandle m_Safety;
            [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Preserve the established static safety ID name.")]
            [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Preserve the established static safety ID name.")]
            internal static readonly SharedStatic<int> s_staticSafetyId = SharedStatic<int>.GetOrCreate<ParallelWriter>();

            internal ParallelWriter(NativeWorkQueue<T> workQueue, ref AtomicSafetyHandle safety)
            {
                _queue = workQueue._queue;
                _queueWriteHead = workQueue._queueWriteHead;
                _currentRef = workQueue._currentRef;
                Capacity = workQueue.Capacity;
                m_Safety = safety;
                CollectionHelper.SetStaticSafetyId<ParallelWriter>(ref m_Safety, ref s_staticSafetyId.Data);
            }
#else
            internal ParallelWriter(NativeWorkQueue<T> workQueue)
            {
                _queue = workQueue._queue;
                _queueWriteHead = workQueue._queueWriteHead;
                _currentRef = workQueue._currentRef;
                Capacity = workQueue.Capacity;
            }
#endif

            public int Capacity { get; }

            public int TryAdd(out T* ptr)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif

                var idx = Interlocked.Increment(ref *_queueWriteHead) - 1;

                if (idx >= Capacity)
                {
                    // we've gone past end of list, don't write we'll requeue this next frame
                    ptr = null;

                    return 0;
                }

                int queueRef;
                do
                {
                    queueRef = Interlocked.Increment(ref *_currentRef);
                }
                while (Hint.Unlikely(queueRef == 0));

                ptr = _queue + idx;
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
            *QueueWriteHead = 0;
            *QueueReadHead = 0;
        }
    }
}
