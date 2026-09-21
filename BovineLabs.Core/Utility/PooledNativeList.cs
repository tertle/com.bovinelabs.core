namespace BovineLabs.Core.Utility
{
    using System;
    using System.Runtime.InteropServices;
    using BovineLabs.Core.Internal;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs.LowLevel.Unsafe;
    using Unity.Scripting.LifecycleManagement;

    public unsafe struct PooledNativeList<T> : IDisposable
        where T : unmanaged
    {
        private NativeList<T> _list;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private AtomicSafetyHandle _oldHandle;
#endif

        public NativeList<T> List => _list;

        private PooledNativeList<T> Create()
        {
            ref var data = ref PooledNativeList.Pool.Data;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (!data.IsCreated)
            {
                throw new InvalidOperationException("PooledNativeList pool not initialized.");
            }
#endif

            ref var lp = ref PooledNativeList.Pool.Data.GetThreadList();
            if (lp.Length == 0)
            {
                // Nothing in the pool, just create a new one
                _list = new NativeList<T>(0, data.Allocator);
            }
            else
            {
                // Pop an existing list out
                var byteList = lp[^1];
                lp.RemoveAt(lp.Length - 1);

                _list = UnsafeUtility.As<NativeList<byte>, NativeList<T>>(ref byteList);
                _list.GetListData()->m_capacity = byteList.Capacity / UnsafeUtility.SizeOf<T>();
            }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            // Replace our safety as it's not valid within the job as we've stored these inside another container so can't be injected
            ref var safety = ref _list.GetSafety();
            _oldHandle = safety;
            safety = AtomicSafetyHandle.Create();
#endif

            return this;
        }

        /// <summary>
        /// Dispose the returned instance to return its storage to the thread-local pool.
        /// </summary>
        public static PooledNativeList<T> Make()
        {
            return default(PooledNativeList<T>).Create();
        }

        public void Dispose()
        {
            if (!_list.IsCreated)
            {
                return;
            }

            ref var lp = ref PooledNativeList.Pool.Data.GetThreadList();

            _list.Clear();

            // Convert back to a byte list
            ref var byteList = ref UnsafeUtility.As<NativeList<T>, NativeList<byte>>(ref _list);
            byteList.GetListData()->m_capacity = _list.Capacity * UnsafeUtility.SizeOf<T>();

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            // Release the Temp handle
            ref var safety = ref byteList.GetSafety();
            AtomicSafetyHandle.CheckDeallocateAndThrow(safety);
            AtomicSafetyHandle.Release(safety);
            safety = _oldHandle;
#endif

            // Only add back to pool if we haven't exceeded the max size
            if (lp.Length < PooledNativeList.MaxPoolSizePerThread)
            {
                lp.Add(byteList);
            }
            else
            {
                // Pool is full, dispose the list instead
                byteList.Dispose();
            }

            _list = default;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            _oldHandle = default;
#endif
        }
    }

    internal static unsafe partial class PooledNativeList
    {
        internal const int MaxPoolSizePerThread = 8;
        internal static readonly SharedStatic<Data> Pool = SharedStatic<Data>.GetOrCreate<Data>();

        [OnCodeLoaded]
        private static void Initialize()
        {
            Pool.Data = new Data(Allocator.Persistent);
        }

        [OnCodeUnloading]
        private static void Shutdown()
        {
            Pool.Data.Dispose();
        }

        internal struct Data
        {
            internal readonly AllocatorManager.AllocatorHandle Allocator;

            [NativeDisableUnsafePtrRestriction]
            private ThreadData* _buffer;

            public Data(AllocatorManager.AllocatorHandle allocator)
            {
                Allocator = allocator;
                _buffer = (ThreadData*)CollectionMemory.Allocate(sizeof(ThreadData) * JobsUtility.ThreadIndexCount, UnsafeUtility.AlignOf<ThreadData>(),
                    allocator);

                for (var i = 0; i < JobsUtility.ThreadIndexCount; i++)
                {
                    _buffer[i].ThreadList = new UnsafeList<NativeList<byte>>(0, Allocator);
                }
            }

            public readonly bool IsCreated => _buffer != null;

            public ref UnsafeList<NativeList<byte>> GetThreadList()
            {
#if UNITY_EDITOR
                UnityEngine.Debug.Assert(JobsUtility.IsExecutingJob || UnityEditorInternal.InternalEditorUtility.CurrentThreadIsMainThread(),
                    "Can only be used on main or worker threads");
#endif

                ref var list = ref UnsafeUtility.ArrayElementAsRef<ThreadData>(_buffer, JobsUtility.ThreadIndex);
                return ref list.ThreadList;
            }

            public void Dispose()
            {
                if (!IsCreated)
                {
                    return;
                }

                for (var i = 0; i < JobsUtility.ThreadIndexCount; i++)
                {
                    foreach (var l in _buffer[i].ThreadList)
                    {
                        l.Dispose();
                    }

                    _buffer[i].ThreadList.Dispose();
                }

                CollectionMemory.Free(_buffer, Allocator);
                _buffer = null;
            }
        }

        [StructLayout(LayoutKind.Explicit, Size = JobsUtility.CacheLineSize)]
        internal struct ThreadData
        {
            [FieldOffset(0)]
            public UnsafeList<NativeList<byte>> ThreadList;
        }
    }
}
