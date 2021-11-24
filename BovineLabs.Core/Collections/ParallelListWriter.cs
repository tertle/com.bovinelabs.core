namespace BovineLabs.Core.Collections
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    public static class ParallelListWriterExt
    {
        public static unsafe ParallelListWriter<T> AsParallelListWriter<T>(this NativeList<T> list)
            where T : struct
        {
            return new ParallelListWriter<T>(list.GetUnsafeList(), ref list.m_Safety);
        }
    }

    // TODO this fixes a bug in NativeList<T>.ParallelWriter
    // Should be removed Unity.Collections release (1.0.0-pre.7)

    /// <summary>
    /// Implements parallel writer. Use AsParallelWriter to obtain it from container.
    /// </summary>
    [NativeContainer]
    [NativeContainerIsAtomicWriteOnly]
    [BurstCompatible(GenericTypeArguments = new[] { typeof(int) })]
    public unsafe struct ParallelListWriter<T>
        where T : struct
    {
        /// <summary>
        ///
        /// </summary>
        [NativeDisableUnsafePtrRestriction]
        public UnsafeList* ListData;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal AtomicSafetyHandle m_Safety;

        internal unsafe ParallelListWriter(UnsafeList* listData, ref AtomicSafetyHandle safety)
        {
            ListData = listData;
            m_Safety = safety;
        }

#else
            internal unsafe ParallelListWriter(UnsafeList* listData)
            {
                ListData = listData;
            }

#endif

        /// <summary>
        /// Adds an element to the list.
        /// </summary>
        /// <param name="value">The value to be added at the end of the list.</param>
        /// <remarks>
        /// If the list has reached its current capacity, internal array won't be resized, and exception will be thrown.
        /// </remarks>
        public void AddNoResize(T value)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
            var idx = Interlocked.Increment(ref ListData->Length) - 1;
            CheckSufficientCapacity(ListData->Capacity, idx + 1);

            UnsafeUtility.WriteArrayElement(ListData->Ptr, idx, value);
        }

        void AddRangeNoResize(int sizeOf, int alignOf, void* ptr, int length)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
            var idx = Interlocked.Add(ref ListData->Length, length) - length;
            CheckSufficientCapacity(ListData->Capacity, idx + length);

            void* dst = (byte*)ListData->Ptr + idx * sizeOf;
            UnsafeUtility.MemCpy(dst, ptr, length * sizeOf);
        }

        /// <summary>
        /// Adds elements from a buffer to this list.
        /// </summary>
        /// <param name="ptr">A pointer to the buffer.</param>
        /// <param name="length">The number of elements to add to the list.</param>
        /// <remarks>
        /// If the list has reached its current capacity, internal array won't be resized, and exception will be thrown.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if length is negative.</exception>
        public void AddRangeNoResize(void* ptr, int length)
        {
            CheckArgPositive(length);
            AddRangeNoResize(UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(), ptr, CollectionHelper.AssumePositive(length));
        }

        /// <summary>
        /// Adds elements from a list to this list.
        /// </summary>
        /// <param name="list">Other container to copy elements from.</param>
        /// <remarks>
        /// If the list has reached its current capacity, internal array won't be resized, and exception will be thrown.
        /// </remarks>
        public void AddRangeNoResize(UnsafeList list)
        {
            AddRangeNoResize(UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(), list.Ptr, list.Length);
        }

        /// <summary>
        /// Adds elements from a list to this list.
        /// </summary>
        /// <param name="list">Other container to copy elements from.</param>
        /// <remarks>
        /// If the list has reached its current capacity, internal array won't be resized, and exception will be thrown.
        /// </remarks>
        public void AddRangeNoResize(NativeList<T> list)
        {
            AddRangeNoResize(*list.m_ListData);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void CheckSufficientCapacity(int capacity, int length)
        {
            if (capacity < length)
            {
                throw new Exception($"Length {length} exceeds capacity Capacity {capacity}");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void CheckArgPositive(int value)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException($"Value {value} must be positive.");
            }
        }
    }
}