// <copyright file="UnsafeListPtr.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Collections
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs;

    [DebuggerDisplay("Length = {Length}")]
    [DebuggerTypeProxy(typeof(UnsafeListPtrDebugView<>))]
    public readonly unsafe struct UnsafeListPtr<T> : INativeDisposable, INativeList<T> // Used by collection initializers.
        where T : unmanaged
    {
        [NativeDisableUnsafePtrRestriction]
        private readonly UnsafeList<T>* listData;

        /// <summary>
        /// Constructs a new list using the specified type of memory allocation.
        /// </summary>
        /// <param name="allocator">A member of the
        /// [Unity.Collections.Allocator](https://docs.unity3d.com/ScriptReference/Unity.Collections.Allocator.html) enumeration.</param>
        /// <remarks>The list initially has a capacity of one. To avoid reallocating memory for the list, specify
        /// sufficient capacity up front.</remarks>
        public UnsafeListPtr(Allocator allocator)
            : this(1, allocator)
        {
        }

        /// <summary>
        /// Constructs a new list with the specified initial capacity and type of memory allocation.
        /// </summary>
        /// <param name="initialCapacity">The initial capacity of the list. If the list grows larger than its capacity,
        /// the internal array is copied to a new, larger array.</param>
        /// <param name="allocator">A member of the
        /// [Unity.Collections.Allocator](https://docs.unity3d.com/ScriptReference/Unity.Collections.Allocator.html) enumeration.</param>
        public UnsafeListPtr(int initialCapacity, Allocator allocator)
        {
            this.listData = UnsafeList<T>.Create(initialCapacity, allocator);
        }

        /// <summary>
        /// Retrieve a member of the contaner by index.
        /// </summary>
        /// <param name="index">The zero-based index into the list.</param>
        /// <value>The list item at the specified index.</value>
        /// <exception cref="IndexOutOfRangeException">Thrown if index is negative or >= to <see cref="Length"/>.</exception>
        public T this[int index]
        {
            get => UnsafeUtility.ReadArrayElement<T>(this.listData->Ptr, CollectionHelper.AssumePositive(index));
            set => UnsafeUtility.WriteArrayElement(this.listData->Ptr, CollectionHelper.AssumePositive(index), value);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public ref T ElementAt(int index)
        {
            return ref UnsafeUtility.ArrayElementAsRef<T>(this.listData->Ptr, index);
        }

        public T* PtrAt(int index)
        {
            return (T*)((byte*)this.listData->Ptr + (UnsafeUtility.SizeOf<T>() * index));
        }

        /// <summary>
        /// The current number of items in the list.
        /// </summary>
        /// <value>The item count.</value>
        public int Length
        {
            get => CollectionHelper.AssumePositive(this.listData->Length);
            set => this.listData->Resize(value, NativeArrayOptions.ClearMemory);
        }

        /// <summary>
        /// The number of items that can fit in the list.
        /// </summary>
        /// <value>The number of items that the list can hold before it resizes its internal storage.</value>
        /// <remarks>Capacity specifies the number of items the list can currently hold. You can change Capacity
        /// to fit more or fewer items. Changing Capacity creates a new array of the specified size, copies the
        /// old array to the new one, and then deallocates the original array memory. You cannot change the Capacity
        /// to a size smaller than <see cref="Length"/> (remove unwanted elements from the list first).</remarks>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if Capacity is set smaller than Length.</exception>
        public int Capacity
        {
            get => CollectionHelper.AssumePositive(this.listData->Capacity);
            set => this.listData->SetCapacity(value);
        }

        public void SetCapacity(int capacity)
        {
            this.Capacity = capacity;
        }

        /// <summary>
        /// Return internal UnsafeList*
        /// </summary>
        /// <returns></returns>
        public UnsafeList<T>* GetUnsafeList() => this.listData;

        /// <summary>
        /// Adds an element to the list.
        /// </summary>
        /// <param name="value">The value to be added at the end of the list.</param>
        /// <remarks>
        /// If the list has reached its current capacity, internal array won't be resized, and exception will be thrown.
        /// </remarks>
        public void AddNoResize(T value)
        {
            this.listData->AddNoResize(value);
        }

        /// <summary>
        /// Adds elements from a Buffer to this list.
        /// </summary>
        /// <param name="ptr">A pointer to the Buffer.</param>
        /// <param name="length">The number of elements to add to the list.</param>
        /// <remarks>
        /// If the list has reached its current capacity, internal array won't be resized, and exception will be thrown.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if length is negative.</exception>
        public void AddRangeNoResize(void* ptr, int length)
        {
            this.listData->AddRangeNoResize(ptr, length);
        }

        /// <summary>
        /// Adds an element to the list.
        /// </summary>
        /// <param name="value">The struct to be added at the end of the list.</param>
        /// <remarks>If the list has reached its current capacity, it copies the original, internal array to
        /// a new, larger array, and then deallocates the original.
        /// </remarks>
        public void Add(in T value)
        {
            this.listData->Add(value);
        }

        /// <summary>
        /// Adds the elements of a NativeArray to this list.
        /// </summary>
        /// <param name="elements">The items to add.</param>
        public void AddRange(NativeArray<T> elements)
        {
            this.AddRange(elements.GetUnsafeReadOnlyPtr(), elements.Length);
        }

        /// <summary>
        /// Adds elements from a Buffer to this list.
        /// </summary>
        /// <param name="elements">A pointer to the Buffer.</param>
        /// <param name="count">The number of elements to add to the list.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if count is negative.</exception>
        public void AddRange(void* elements, int count)
        {
            this.listData->AddRange(elements, CollectionHelper.AssumePositive(count));
        }

        /// <summary>
        /// Inserts a number of items into a container at a specified zero-based index.
        /// </summary>
        /// <param name="begin">The zero-based index at which the new elements should be inserted.</param>
        /// <param name="end">The zero-based index just after where the elements should be removed.</param>
        /// <exception cref="ArgumentException">Thrown if end argument is less than begin argument.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if begin or end arguments are not positive or out of bounds.</exception>
        public void InsertRangeWithBeginEnd(int begin, int end)
        {
            this.listData->InsertRangeWithBeginEnd(CollectionHelper.AssumePositive(begin), CollectionHelper.AssumePositive(end));
        }

        /// <summary>
        /// Truncates the list by replacing the item at the specified index with the last item in the list. The list
        /// is shortened by one.
        /// </summary>
        /// <param name="index">The index of the item to delete.</param>
        /// <exception cref="ArgumentOutOfRangeException">If index is negative or >= <see cref="Length"/>.</exception>
        public void RemoveAtSwapBack(int index)
        {
            this.listData->RemoveAtSwapBack(CollectionHelper.AssumePositive(index));
        }

        /// <summary>
        /// Truncates the list by replacing the item at the specified index range with the items from the end the list. The list
        /// is shortened by number of elements in range.
        /// </summary>
        /// <param name="index">The index of the first element to overwrite.</param>
        /// <param name="count">The number of elements to copy and remove.</param>
        /// <exception cref="ArgumentException">Thrown if end argument is less than begin argument.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if begin or end arguments are not positive or out of bounds.</exception>
        public void RemoveRangeSwapBack(int index, int count)
        {
            this.listData->RemoveRangeSwapBack(CollectionHelper.AssumePositive(index), CollectionHelper.AssumePositive(count));
        }

        /// <summary>
        /// Truncates the list by removing the item at the specified index, and shifting all remaining items to replace removed item. The list
        /// is shortened by one.
        /// </summary>
        /// <param name="index">The index of the item to delete.</param>
        /// <remarks>
        /// This method of removing item is useful only in case when list is ordered and user wants to preserve order
        /// in list after removal In majority of cases is not important and user should use more performant `RemoveAtSwapBack`.
        /// </remarks>
        public void RemoveAt(int index)
        {
            this.listData->RemoveAt(CollectionHelper.AssumePositive(index));
        }

        /// <summary>
        /// Reports whether container is empty.
        /// </summary>
        /// <value>True if this container empty.</value>
        public bool IsEmpty => !this.IsCreated || this.Length == 0;

        /// <summary>
        /// Reports whether memory for the container is allocated.
        /// </summary>
        /// <value>True if this container object's internal storage has been allocated.</value>
        /// <remarks>
        /// Note that the container storage is not created if you use the default constructor. You must specify
        /// at least an allocation type to construct a usable container.
        ///
        /// *Warning:* the `IsCreated` property can't be used to determine whether a copy of a container is still valid.
        /// If you dispose any copy of the container, the container storage is deallocated. However, the properties of
        /// the other copies of the container (including the original) are not updated. As a result the `IsCreated` property
        /// of the copies still return `true` even though the container storage has been deallocated.
        /// Accessing the data of a native container that has been disposed throws a <see cref='InvalidOperationException'/> exception.
        /// </remarks>
        public bool IsCreated => this.listData != null;

        /// <summary>
        /// Disposes of this container and deallocates its memory immediately.
        /// </summary>
        public void Dispose()
        {
            UnsafeList<T>.Destroy(this.listData);
        }

        /// <summary>
        /// Safely disposes of this container and deallocates its memory when the jobs that use it have completed.
        /// </summary>
        /// <remarks>You can call this function dispose of the container immediately after scheduling the job. Pass
        /// the [JobHandle](https://docs.unity3d.com/ScriptReference/Unity.Jobs.JobHandle.html) returned by
        /// the [Job.Schedule](https://docs.unity3d.com/ScriptReference/Unity.Jobs.IJobExtensions.Schedule.html)
        /// method using the `jobHandle` parameter so the job scheduler can dispose the container after all jobs
        /// using it have run.</remarks>
        /// <param name="inputDeps">The job handle or handles for any scheduled jobs that use this container.</param>
        /// <returns>A new job handle containing the prior handles as well as the handle for the job that deletes
        /// the container.</returns>
        [BurstCompatible(RequiredUnityDefine = "UNITY_2020_2_OR_NEWER") /* Due to job scheduling on 2020.1 using statics */]
        public JobHandle Dispose(JobHandle inputDeps)
        {
            var jobHandle = new NativeListDisposeJob { Data = new NativeListDispose { m_ListData = this.listData } }.Schedule(inputDeps);
            return jobHandle;
        }

        /// <summary>
        /// Clears the list.
        /// </summary>
        /// <remarks>List <see cref="Capacity"/> remains unchanged.</remarks>
        public void Clear()
        {
            this.listData->Clear();
        }

        /// <summary>
        /// Changes the list length, resizing if necessary.
        /// </summary>
        /// <param name="length">The new length of the list.</param>
        /// <param name="options">Memory should be cleared on allocation or left uninitialized.</param>
        public void Resize(int length, NativeArrayOptions options)
        {
            this.listData->Resize(length, options);
        }

        /// <summary>
        /// Changes the container length, resizing if necessary, without initializing memory.
        /// </summary>
        /// <param name="length">The new length of the container.</param>
        public void ResizeUninitialized(int length)
        {
            this.Resize(length, NativeArrayOptions.UninitializedMemory);
        }

        public NativeArray<T> AsArray()
        {
            var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(this.listData->Ptr, this.listData->Length, Allocator.None);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, AtomicSafetyHandle.GetTempMemoryHandle());
#endif
            return array;
        }

        /// <summary> A copy of this list as a managed array.. </summary>
        /// <returns>A ManagedArray containing copies of all the items in the list.</returns>
        [NotBurstCompatible]
        public T[] ToArray()
        {
            return this.AsArray().ToArray();
        }

        /// <summary>
        /// Returns parallel writer instance.
        /// </summary>
        /// <returns>Parallel writer instance.</returns>
        public ParallelWriter AsParallelWriter()
        {
            return new ParallelWriter(this.listData);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void CheckReinterpretSize<U>(long tSize, long uSize, int expectedTypeSize, long byteLen, long uLen)
        {
            if (tSize != expectedTypeSize)
            {
                throw new InvalidOperationException($"Type {typeof(T)} was expected to be {expectedTypeSize} but is {tSize} bytes");
            }

            if (uLen * uSize != byteLen)
            {
                throw new InvalidOperationException(
                    $"Types {typeof(T)} (array length {this.Length}) and {typeof(U)} cannot be aliased due to size constraints. The size of the types and lengths involved must line up.");
            }
        }

        /// <summary>
        /// Implements parallel writer. Use AsParallelWriter to obtain it from container.
        /// </summary>
        [BurstCompatible(GenericTypeArguments = new [] { typeof(int) })]
        public struct ParallelWriter
        {
            /// <summary>
            ///
            /// </summary>
            [NativeDisableUnsafePtrRestriction]
            public UnsafeList<T>* ListData;

            internal ParallelWriter(UnsafeList<T>* listData)
            {
                this.ListData = listData;
            }

            /// <summary>
            /// Adds an element to the list.
            /// </summary>
            /// <param name="value">The value to be added at the end of the list.</param>
            /// <remarks>
            /// If the list has reached its current capacity, internal array won't be resized, and exception will be thrown.
            /// </remarks>
            public void AddNoResize(T value)
            {
                var idx = Interlocked.Increment(ref this.ListData->m_length) - 1;
                CheckSufficientCapacity(this.ListData->Capacity, idx + 1);

                UnsafeUtility.WriteArrayElement(this.ListData->Ptr, idx, value);
            }

            void AddRangeNoResize(int sizeOf, int alignOf, void* ptr, int length)
            {
                var idx = Interlocked.Add(ref this.ListData->m_length, length) - length;
                CheckSufficientCapacity(this.ListData->Capacity, idx + length);

                void* dst = (byte*)this.ListData->Ptr + (idx * sizeOf);
                UnsafeUtility.MemCpy(dst, ptr, length * sizeOf);
            }

            /// <summary>
            /// Adds elements from a Buffer to this list.
            /// </summary>
            /// <param name="ptr">A pointer to the Buffer.</param>
            /// <param name="length">The number of elements to add to the list.</param>
            /// <remarks>
            /// If the list has reached its current capacity, internal array won't be resized, and exception will be thrown.
            /// </remarks>
            /// <exception cref="ArgumentOutOfRangeException">Thrown if length is negative.</exception>
            public void AddRangeNoResize(void* ptr, int length)
            {
                this.AddRangeNoResize(UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(), ptr, CollectionHelper.AssumePositive(length));
            }

            /// <summary>
            /// Adds elements from a list to this list.
            /// </summary>
            /// <param name="list">Other container to copy elements from.</param>
            /// <remarks>
            /// If the list has reached its current capacity, internal array won't be resized, and exception will be thrown.
            /// </remarks>
            public void AddRangeNoResize(UnsafeList<T> list)
            {
                this.AddRangeNoResize(UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(), list.Ptr, list.Length);
            }

            public void Reserve(int length, out T* ptr, out int idx)
            {
                idx = Interlocked.Add(ref this.ListData->m_length, length) - length;
                ptr = (T*)((byte*)this.ListData->Ptr + (idx * UnsafeUtility.SizeOf<T>()));
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void CheckSufficientCapacity(int capacity, int length)
        {
            if (capacity < length)
            {
                throw new Exception($"Length {length} exceeds capacity Capacity {capacity}");
            }
        }

        [NativeContainer]
        [BurstCompatible]
        internal struct NativeListDispose
        {
            [NativeDisableUnsafePtrRestriction]
            public UnsafeList<T>* m_ListData;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            internal AtomicSafetyHandle m_Safety;
#endif

            public void Dispose()
            {
                UnsafeList<T>.Destroy(this.m_ListData);
            }
        }

        [BurstCompile]
        [BurstCompatible]
        internal struct NativeListDisposeJob : IJob
        {
            internal NativeListDispose Data;

            public void Execute()
            {
                this.Data.Dispose();
            }
        }
    }

    sealed class UnsafeListPtrDebugView<T> where T : unmanaged
    {
        private UnsafeListPtr<T> array;

        public UnsafeListPtrDebugView(UnsafeListPtr<T> array)
        {
            this.array = array;
        }

        public T[] Items => this.array.ToArray();
    }
}