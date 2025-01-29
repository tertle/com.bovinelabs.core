// <copyright file="UnsafeArray.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Collections
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs;
    using UnityEngine.Internal;

    /// <summary> An unsafe version of <see cref="NativeArray{T}" />. </summary>
    /// <typeparam name="T"> The type that array holds. </typeparam>
    [DebuggerTypeProxy(typeof(UnsafeArray<>.UnsafeArrayDebugView))]
    [DebuggerDisplay("Length = {Length}")]
    public struct UnsafeArray<T> : IDisposable, IEnumerable<T>, IEquatable<UnsafeArray<T>>
        where T : unmanaged
    {
        [NativeDisableUnsafePtrRestriction]
        private unsafe void* buffer;

        private Allocator allocatorLabel;

        public unsafe UnsafeArray(int length, Allocator allocator, NativeArrayOptions options = NativeArrayOptions.ClearMemory)
        {
            Allocate(length, allocator, out this);
            if (options != NativeArrayOptions.ClearMemory)
            {
                return;
            }

            UnsafeUtility.MemClear(this.buffer, this.Length * (long)UnsafeUtility.SizeOf<T>());
        }

        public int Length { get; private set; }

        public unsafe bool IsCreated => (IntPtr)this.buffer != IntPtr.Zero;

        public unsafe T this[int index]
        {
            get => UnsafeUtility.ReadArrayElement<T>(this.buffer, index);
            [WriteAccessRequired]
            set => UnsafeUtility.WriteArrayElement(this.buffer, index, value);
        }

        public static bool operator ==(UnsafeArray<T> left, UnsafeArray<T> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(UnsafeArray<T> left, UnsafeArray<T> right)
        {
            return !left.Equals(right);
        }

        [WriteAccessRequired]
        public unsafe void Dispose()
        {
            if ((IntPtr)this.buffer == IntPtr.Zero)
            {
                throw new ObjectDisposedException("The UnsafeArray is already disposed.");
            }

            if (this.allocatorLabel == Allocator.Invalid)
            {
                throw new InvalidOperationException("The UnsafeArray can not be Disposed because it was not allocated with a valid allocator.");
            }

            if (this.allocatorLabel > Allocator.None)
            {
                UnsafeUtility.FreeTracked(this.buffer, this.allocatorLabel);
                this.allocatorLabel = Allocator.Invalid;
            }

            this.buffer = null;
        }

        public unsafe JobHandle Dispose(JobHandle inputDeps)
        {
            if (this.allocatorLabel == Allocator.Invalid)
            {
                throw new InvalidOperationException("The UnsafeArray can not be Disposed because it was not allocated with a valid allocator.");
            }

            if ((IntPtr)this.buffer == IntPtr.Zero)
            {
                throw new InvalidOperationException("The UnsafeArray is already disposed.");
            }

            if (this.allocatorLabel > Allocator.None)
            {
                var jobHandle = new UnsafeArrayDisposeJob
                {
                    Data = new UnsafeArrayDispose
                    {
                        Buffer = this.buffer,
                        AllocatorLabel = this.allocatorLabel,
                    },
                }.Schedule(inputDeps);

                this.buffer = null;
                this.allocatorLabel = Allocator.Invalid;
                return jobHandle;
            }

            this.buffer = null;
            return inputDeps;
        }

        public unsafe void* GetUnsafePtr()
        {
            return this.buffer;
        }

        [WriteAccessRequired]
        public void CopyFrom(T[] array)
        {
            Copy(array, this);
        }

        [WriteAccessRequired]
        public void CopyFrom(UnsafeArray<T> array)
        {
            Copy(array, this);
        }

        public void CopyTo(T[] array)
        {
            Copy(this, array);
        }

        public void CopyTo(UnsafeArray<T> array)
        {
            Copy(this, array);
        }

        public T[] ToArray()
        {
            var dst = new T[this.Length];
            Copy(this, dst, this.Length);
            return dst;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(ref this);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return new Enumerator(ref this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public unsafe bool Equals(UnsafeArray<T> other)
        {
            return this.buffer == other.buffer && this.Length == other.Length;
        }

        public override bool Equals(object obj)
        {
            return obj != null && obj is UnsafeArray<T> other && this.Equals(other);
        }

        public override unsafe int GetHashCode()
        {
            return ((int)this.buffer * 397) ^ this.Length;
        }

        public static void Copy(UnsafeArray<T> src, UnsafeArray<T> dst)
        {
            CheckCopyLengths(src.Length, dst.Length);
            CopySafe(src, 0, dst, 0, src.Length);
        }

        public static void Copy(T[] src, UnsafeArray<T> dst)
        {
            CheckCopyLengths(src.Length, dst.Length);
            CopySafe(src, 0, dst, 0, src.Length);
        }

        public static void Copy(UnsafeArray<T> src, T[] dst)
        {
            CheckCopyLengths(src.Length, dst.Length);
            CopySafe(src, 0, dst, 0, src.Length);
        }

        public static void Copy(UnsafeArray<T> src, UnsafeArray<T> dst, int length)
        {
            CopySafe(src, 0, dst, 0, length);
        }

        public static void Copy(T[] src, UnsafeArray<T> dst, int length)
        {
            CopySafe(src, 0, dst, 0, length);
        }

        public static void Copy(UnsafeArray<T> src, T[] dst, int length)
        {
            CopySafe(src, 0, dst, 0, length);
        }

        public static void Copy(UnsafeArray<T> src, int srcIndex, UnsafeArray<T> dst, int dstIndex, int length)
        {
            CopySafe(src, srcIndex, dst, dstIndex, length);
        }

        public static void Copy(T[] src, int srcIndex, UnsafeArray<T> dst, int dstIndex, int length)
        {
            CopySafe(src, srcIndex, dst, dstIndex, length);
        }

        public static void Copy(UnsafeArray<T> src, int srcIndex, T[] dst, int dstIndex, int length)
        {
            CopySafe(src, srcIndex, dst, dstIndex, length);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckAllocateArguments(int length, Allocator allocator)
        {
            if (allocator <= Allocator.None)
            {
                throw new ArgumentException("Allocator must be Temp, TempJob or Persistent", nameof(allocator));
            }

            if (allocator >= Allocator.FirstUserIndex)
            {
                throw new ArgumentException("Use CollectionHelper.CreateUnsafeArray for custom allocator", nameof(allocator));
            }

            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length), "Length must be >= 0");
            }
        }

        private static unsafe void Allocate(int length, Allocator allocator, out UnsafeArray<T> array)
        {
            var size = UnsafeUtility.SizeOf<T>() * (long)length;
            CheckAllocateArguments(length, allocator);
            array = default;
            IsUnmanagedAndThrow();
            array.buffer = UnsafeUtility.MallocTracked(size, UnsafeUtility.AlignOf<T>(), allocator, 0);
            array.Length = length;
            array.allocatorLabel = allocator;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [BurstDiscard]
        private static void IsUnmanagedAndThrow()
        {
            if (!UnsafeUtility.IsUnmanaged<T>())
            {
                throw new InvalidOperationException($"{typeof(T)} used in UnsafeArray<{typeof(T)}> must be unmanaged (contain no managed types).");
            }
        }

        private static unsafe void CopySafe(UnsafeArray<T> src, int srcIndex, UnsafeArray<T> dst, int dstIndex, int length)
        {
            CheckCopyArguments(src.Length, srcIndex, dst.Length, dstIndex, length);
            UnsafeUtility.MemCpy((void*)((IntPtr)dst.buffer + (dstIndex * UnsafeUtility.SizeOf<T>())),
                (void*)((IntPtr)src.buffer + (srcIndex * UnsafeUtility.SizeOf<T>())), length * UnsafeUtility.SizeOf<T>());
        }

        private static unsafe void CopySafe(T[] src, int srcIndex, UnsafeArray<T> dst, int dstIndex, int length)
        {
            CheckCopyPtr(src);
            CheckCopyArguments(src.Length, srcIndex, dst.Length, dstIndex, length);
            var gcHandle = GCHandle.Alloc(src, GCHandleType.Pinned);
            var num = gcHandle.AddrOfPinnedObject();
            UnsafeUtility.MemCpy((void*)((IntPtr)dst.buffer + (dstIndex * UnsafeUtility.SizeOf<T>())),
                (void*)((IntPtr)(void*)num + (srcIndex * UnsafeUtility.SizeOf<T>())), length * UnsafeUtility.SizeOf<T>());

            gcHandle.Free();
        }

        private static unsafe void CopySafe(UnsafeArray<T> src, int srcIndex, T[] dst, int dstIndex, int length)
        {
            CheckCopyPtr(dst);
            CheckCopyArguments(src.Length, srcIndex, dst.Length, dstIndex, length);
            var gcHandle = GCHandle.Alloc(dst, GCHandleType.Pinned);
            UnsafeUtility.MemCpy((void*)((IntPtr)(void*)gcHandle.AddrOfPinnedObject() + (dstIndex * UnsafeUtility.SizeOf<T>())),
                (void*)((IntPtr)src.buffer + (srcIndex * UnsafeUtility.SizeOf<T>())), length * UnsafeUtility.SizeOf<T>());

            gcHandle.Free();
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckCopyPtr(T[] ptr)
        {
            if (ptr == null)
            {
                throw new ArgumentNullException(nameof(ptr));
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckCopyLengths(int srcLength, int dstLength)
        {
            if (srcLength != dstLength)
            {
                throw new ArgumentException("source and destination length must be the same");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckCopyArguments(int srcLength, int srcIndex, int dstLength, int dstIndex, int length)
        {
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length), "length must be equal or greater than zero.");
            }

            if (srcIndex < 0 || srcIndex > srcLength || (srcIndex == srcLength && srcLength > 0))
            {
                throw new ArgumentOutOfRangeException(nameof(srcIndex), "srcIndex is outside the range of valid indexes for the source UnsafeArray.");
            }

            if (dstIndex < 0 || dstIndex > dstLength || (dstIndex == dstLength && dstLength > 0))
            {
                throw new ArgumentOutOfRangeException(nameof(dstIndex), "dstIndex is outside the range of valid indexes for the destination UnsafeArray.");
            }

            if (srcIndex + length > srcLength)
            {
                throw new ArgumentException("length is greater than the number of elements from srcIndex to the end of the source UnsafeArray.",
                    nameof(length));
            }

            if (srcIndex + length < 0)
            {
                throw new ArgumentException("srcIndex + length causes an integer overflow");
            }

            if (dstIndex + length > dstLength)
            {
                throw new ArgumentException("length is greater than the number of elements from dstIndex to the end of the destination UnsafeArray.",
                    nameof(length));
            }

            if (dstIndex + length < 0)
            {
                throw new ArgumentException("dstIndex + length causes an integer overflow");
            }
        }

        [ExcludeFromDocs]
        public struct Enumerator : IEnumerator<T>
        {
            private UnsafeArray<T> array;
            private int index;

            public Enumerator(ref UnsafeArray<T> array)
            {
                this.array = array;
                this.index = -1;
            }

            public T Current => this.array[this.index];

            object IEnumerator.Current => this.Current;

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                ++this.index;
                return this.index < this.array.Length;
            }

            public void Reset()
            {
                this.index = -1;
            }
        }

        private struct UnsafeArrayDisposeJob : IJob
        {
            internal UnsafeArrayDispose Data;

            public void Execute()
            {
                this.Data.Dispose();
            }
        }

        [NativeContainer]
        private struct UnsafeArrayDispose
        {
            [NativeDisableUnsafePtrRestriction]
            internal unsafe void* Buffer;

            internal Allocator AllocatorLabel;

            public unsafe void Dispose()
            {
                UnsafeUtility.FreeTracked(this.Buffer, this.AllocatorLabel);
            }
        }

        private sealed class UnsafeArrayDebugView
        {
            private UnsafeArray<T> array;

            public UnsafeArrayDebugView(UnsafeArray<T> array)
            {
                this.array = array;
            }

            public T[] Items => this.array.ToArray();
        }
    }
}
