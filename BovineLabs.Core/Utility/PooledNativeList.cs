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
        private NativeList<T> list;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private AtomicSafetyHandle oldHandle;
#endif

        public NativeList<T> List => this.list;

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
                this.list = new NativeList<T>(0, data.Allocator);
            }
            else
            {
                // Pop an existing list out
                var byteList = lp[^1];
                lp.RemoveAt(lp.Length - 1);

                this.list = UnsafeUtility.As<NativeList<byte>, NativeList<T>>(ref byteList);
                this.list.GetListData()->m_capacity = byteList.Capacity / UnsafeUtility.SizeOf<T>();
            }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            // Replace our safety as it's not valid within the job as we've stored these inside another container so can't be injected
            this.oldHandle = this.list.GetSafety();
            this.list.GetSafety() = AtomicSafetyHandle.Create();
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
            if (!this.list.IsCreated)
            {
                return;
            }

            ref var lp = ref PooledNativeList.Pool.Data.GetThreadList();

            this.list.Clear();

            // Convert back to a byte list
            ref var byteList = ref UnsafeUtility.As<NativeList<T>, NativeList<byte>>(ref this.list);
            byteList.GetListData()->m_capacity = this.list.Capacity * UnsafeUtility.SizeOf<T>();

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            // Release the Temp handle
            AtomicSafetyHandle.CheckDeallocateAndThrow(byteList.GetSafety());
            AtomicSafetyHandle.Release(byteList.GetSafety());
            byteList.GetSafety() = this.oldHandle;
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

            this.list = default;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            this.oldHandle = default;
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
            private ThreadData* buffer;

            public Data(AllocatorManager.AllocatorHandle allocator)
            {
                this.Allocator = allocator;
                this.buffer = (ThreadData*)CollectionMemory.Allocate(sizeof(ThreadData) * JobsUtility.ThreadIndexCount, UnsafeUtility.AlignOf<ThreadData>(),
                    allocator);

                for (var i = 0; i < JobsUtility.ThreadIndexCount; i++)
                {
                    this.buffer[i].ThreadList = new UnsafeList<NativeList<byte>>(0, this.Allocator);
                }
            }

            public readonly bool IsCreated => this.buffer != null;

            public ref UnsafeList<NativeList<byte>> GetThreadList()
            {
#if UNITY_EDITOR
                UnityEngine.Debug.Assert(JobsUtility.IsExecutingJob || UnityEditorInternal.InternalEditorUtility.CurrentThreadIsMainThread(),
                    "Can only be used on main or worker threads");
#endif

                ref var list = ref UnsafeUtility.ArrayElementAsRef<ThreadData>(this.buffer, JobsUtility.ThreadIndex);
                return ref list.ThreadList;
            }

            public void Dispose()
            {
                if (!this.IsCreated)
                {
                    return;
                }

                for (var i = 0; i < JobsUtility.ThreadIndexCount; i++)
                {
                    foreach (var l in this.buffer[i].ThreadList)
                    {
                        l.Dispose();
                    }

                    this.buffer[i].ThreadList.Dispose();
                }

                CollectionMemory.Free(this.buffer, this.Allocator);
                this.buffer = null;
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
