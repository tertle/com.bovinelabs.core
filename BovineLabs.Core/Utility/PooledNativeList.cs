// <copyright file="PooledNativeList.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Utility
{
    using System;
    using System.Runtime.InteropServices;
    using Unity;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs.LowLevel.Unsafe;

    /// <summary>
    /// A pooled wrapper around NativeList that reuses allocated memory across instances to reduce allocation pressure.
    /// Uses thread-local pools to avoid contention in multi-threaded scenarios.
    /// </summary>
    /// <typeparam name="T">The unmanaged element type.</typeparam>
    public unsafe struct PooledNativeList<T> : IDisposable
        where T : unmanaged
    {
        private NativeList<T> list;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private AtomicSafetyHandle oldHandle;
#endif

        /// <summary>
        /// Gets the underlying NativeList instance.
        /// </summary>
        /// <value>The wrapped NativeList that can be used for all list operations.</value>
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
                this.list.m_ListData->m_capacity = byteList.Capacity / UnsafeUtility.SizeOf<T>();
            }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            // Replace our safety as it's not valid within the job as we've stored these inside another container so can't be injected
            this.oldHandle = this.list.m_Safety;
            this.list.m_Safety = AtomicSafetyHandle.Create();
#endif

            return this;
        }

        /// <summary>
        /// Creates a new PooledNativeList instance, either from the thread-local pool or by allocating a new one.
        /// </summary>
        /// <returns>A PooledNativeList instance ready for use.</returns>
        /// <remarks>
        /// This method is thread-safe and will reuse previously disposed instances when available.
        /// The returned instance must be disposed to return it to the pool.
        /// </remarks>
        public static PooledNativeList<T> Make()
        {
            return default(PooledNativeList<T>).Create();
        }

        /// <summary>
        /// Disposes the PooledNativeList and returns the underlying memory to the thread-local pool for reuse.
        /// </summary>
        /// <remarks>
        /// This method clears the list contents and converts it back to a byte list for storage in the pool.
        /// The instance should not be used after disposal.
        /// </remarks>
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
            byteList.m_ListData->m_capacity = this.list.Capacity * UnsafeUtility.SizeOf<T>();

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            // Release the Temp handle
            AtomicSafetyHandle.CheckDeallocateAndThrow(byteList.m_Safety);
            AtomicSafetyHandle.Release(byteList.m_Safety);
            byteList.m_Safety = this.oldHandle;
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

    internal static unsafe class PooledNativeList
    {
        internal const int MaxPoolSizePerThread = 8;
        internal static readonly SharedStatic<Data> Pool = SharedStatic<Data>.GetOrCreate<Data>();

        /// <summary>
        /// Initializes the global pool data structure used by all PooledNativeList instances.
        /// </summary>
        /// <remarks>
        /// This method is called automatically during Unity initialization and should not be called manually.
        /// Creates thread-local storage for each worker thread to avoid contention.
        /// </remarks>
#if !UNITY_EDITOR
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
#endif
        public static void Initialize()
        {
            if (Pool.Data.IsCreated)
            {
                return;
            }

            Pool.Data = new Data(Allocator.Domain);
        }

        internal struct Data
        {
            internal readonly AllocatorManager.AllocatorHandle Allocator;

            [NativeDisableUnsafePtrRestriction]
            private ThreadData* buffer;

            public Data(AllocatorManager.AllocatorHandle allocator)
            {
                this.Allocator = allocator;
                this.buffer = (ThreadData*)Memory.Unmanaged.Allocate(sizeof(ThreadData) * JobsUtility.ThreadIndexCount, UnsafeUtility.AlignOf<ThreadData>(),
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

                Memory.Unmanaged.Free(this.buffer, this.Allocator);
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
