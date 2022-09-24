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
    using JetBrains.Annotations;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs;
    using UnityEngine.Internal;

    /// <summary>
    /// A UnsafeArray exposes a Buffer of native memory to managed code, making it possible to share data between managed and native without marshalling costs.
    /// </summary>
    /// <typeparam name="T"> The type of data the array holds. </typeparam>
    [DebuggerDisplay("Length = {" + nameof(Length) + "}")]
    [DebuggerTypeProxy(typeof(UnsafeArrayDebugView<>))]
    public unsafe struct UnsafeArray<T> : IDisposable, IEnumerable<T>, IEquatable<UnsafeArray<T>>
        where T : struct
    {
        [NativeDisableUnsafePtrRestriction]
        private void* buffer;

        private int length;
        private Allocator allocatorLabel;

        public UnsafeArray(int length, Allocator allocator, NativeArrayOptions options = NativeArrayOptions.ClearMemory)
        {
            Allocate(length, allocator, out this);
            if (options == NativeArrayOptions.UninitializedMemory)
            {
                return;
            }

            UnsafeUtility.MemClear(this.buffer, UnsafeUtility.SizeOf<T>() * (long)this.Length);
        }

        public UnsafeArray(T[] array, Allocator allocator)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            Allocate(array.Length, allocator, out this);
            Copy(array, this);
        }

        public UnsafeArray(UnsafeArray<T> array, Allocator allocator)
        {
            Allocate(array.Length, allocator, out this);
            Copy(array, 0, this, 0, array.Length);
        }

        public UnsafeArray(void* dataPointer, int length, Allocator allocator)
        {
            CheckConvertArguments(length);

            this.buffer = dataPointer;
            this.length = length;
            this.allocatorLabel = allocator;
        }

        public int Length => this.length;

        public bool IsCreated => (IntPtr)this.buffer != IntPtr.Zero;

        public T this[int index]
        {
            get
            {
                this.CheckIndexRange(index);
                return UnsafeUtility.ReadArrayElement<T>(this.buffer, index);
            }

            [WriteAccessRequired]
            set
            {
                this.CheckIndexRange(index);
                UnsafeUtility.WriteArrayElement(this.buffer, index, value);
            }
        }

        public static bool operator ==(UnsafeArray<T> left, UnsafeArray<T> right) => left.Equals(right);

        public static bool operator !=(UnsafeArray<T> left, UnsafeArray<T> right) => !left.Equals(right);

        public static void Copy(UnsafeArray<T> src, UnsafeArray<T> dst)
        {
            CheckCopyLengths(src.Length, dst.Length);
            Copy(src, 0, dst, 0, src.Length);
        }

        public static void Copy(UnsafeArray<T>.ReadOnly src, UnsafeArray<T> dst)
        {
            CheckCopyLengths(src.Length, dst.Length);
            Copy(src, 0, dst, 0, src.Length);
        }

        public static void Copy(T[] src, UnsafeArray<T> dst)
        {
            CheckCopyLengths(src.Length, dst.Length);
            Copy(src, 0, dst, 0, src.Length);
        }

        public static void Copy(UnsafeArray<T> src, T[] dst)
        {
            CheckCopyLengths(src.Length, dst.Length);
            Copy(src, 0, dst, 0, src.Length);
        }

        public static void Copy(UnsafeArray<T>.ReadOnly src, T[] dst)
        {
            CheckCopyLengths(src.Length, dst.Length);
            Copy(src, 0, dst, 0, src.Length);
        }

        public static void Copy(UnsafeArray<T> src, UnsafeArray<T> dst, int length) => Copy(src, 0, dst, 0, length);

        public static void Copy(UnsafeArray<T>.ReadOnly src, UnsafeArray<T> dst, int length) => Copy(src, 0, dst, 0, length);

        public static void Copy(T[] src, UnsafeArray<T> dst, int length) => Copy(src, 0, dst, 0, length);

        public static void Copy(UnsafeArray<T> src, T[] dst, int length) => Copy(src, 0, dst, 0, length);

        public static void Copy(UnsafeArray<T>.ReadOnly src, T[] dst, int length) => Copy(src, 0, dst, 0, length);

        public static void Copy(UnsafeArray<T> src, int srcIndex, UnsafeArray<T> dst, int dstIndex, int length)
        {
            CheckCopyArguments(src.Length, srcIndex, dst.Length, dstIndex, length);
            UnsafeUtility.MemCpy(
                (void*)((IntPtr)dst.buffer + (dstIndex * UnsafeUtility.SizeOf<T>())),
                (void*)((IntPtr)src.buffer + (srcIndex * UnsafeUtility.SizeOf<T>())),
                UnsafeUtility.SizeOf<T>() * length);
        }

        public static void Copy(UnsafeArray<T>.ReadOnly src, int srcIndex, UnsafeArray<T> dst, int dstIndex, int length)
        {
            CheckCopyArguments(src.Length, srcIndex, dst.Length, dstIndex, length);
            UnsafeUtility.MemCpy(
                (void*)((IntPtr)dst.buffer + (dstIndex * UnsafeUtility.SizeOf<T>())),
                (void*)((IntPtr)src.Buffer + (srcIndex * UnsafeUtility.SizeOf<T>())),
                UnsafeUtility.SizeOf<T>() * length);
        }

        public static void Copy(T[] src, int srcIndex, UnsafeArray<T> dst, int dstIndex, int length)
        {
            if (src == null)
            {
                throw new ArgumentNullException(nameof(src));
            }

            CheckCopyArguments(src.Length, srcIndex, dst.Length, dstIndex, length);
            var gcHandle = GCHandle.Alloc(src, GCHandleType.Pinned);
            var num = gcHandle.AddrOfPinnedObject();
            UnsafeUtility.MemCpy(
                (void*)((IntPtr)dst.buffer + (dstIndex * UnsafeUtility.SizeOf<T>())),
                (void*)((IntPtr)(void*)num + (srcIndex * UnsafeUtility.SizeOf<T>())),
                UnsafeUtility.SizeOf<T>() * length);
            gcHandle.Free();
        }

        public static void Copy(UnsafeArray<T> src, int srcIndex, T[] dst, int dstIndex, int length)
        {
            if (dst == null)
            {
                throw new ArgumentNullException(nameof(dst));
            }

            CheckCopyArguments(src.Length, srcIndex, dst.Length, dstIndex, length);
            var gcHandle = GCHandle.Alloc(dst, GCHandleType.Pinned);
            UnsafeUtility.MemCpy(
                (void*)((IntPtr)(void*)gcHandle.AddrOfPinnedObject() + (dstIndex * UnsafeUtility.SizeOf<T>())),
                (void*)((IntPtr)src.buffer + (srcIndex * UnsafeUtility.SizeOf<T>())),
                UnsafeUtility.SizeOf<T>() * length);
            gcHandle.Free();
        }

        public static void Copy(UnsafeArray<T>.ReadOnly src, int srcIndex, T[] dst, int dstIndex, int length)
        {
            if (dst == null)
            {
                throw new ArgumentNullException(nameof(dst));
            }

            CheckCopyArguments(src.Length, srcIndex, dst.Length, dstIndex, length);
            var gcHandle = GCHandle.Alloc(dst, GCHandleType.Pinned);
            UnsafeUtility.MemCpy(
                (void*)((IntPtr)(void*)gcHandle.AddrOfPinnedObject() + (dstIndex * UnsafeUtility.SizeOf<T>())),
                (void*)((IntPtr)src.Buffer + (srcIndex * UnsafeUtility.SizeOf<T>())),
                length * UnsafeUtility.SizeOf<T>());
            gcHandle.Free();
        }

        [WriteAccessRequired]
        public void Dispose()
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
                UnsafeUtility.Free(this.buffer, this.allocatorLabel);
                this.allocatorLabel = Allocator.Invalid;
            }

            this.buffer = null;
            this.length = 0;
        }

        public JobHandle Dispose(JobHandle inputDeps)
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
                var jobHandle = new UnsafeArrayDisposeJob()
                {
                    Data = new UnsafeArrayDispose
                    {
                        Buffer = this.buffer,
                        AllocatorLabel = this.allocatorLabel,
                    },
                }.Schedule(inputDeps);

                this.buffer = null;
                this.length = 0;
                this.allocatorLabel = Allocator.Invalid;
                return jobHandle;
            }

            this.buffer = null;
            this.length = 0;
            return inputDeps;
        }

        [WriteAccessRequired]
        public void CopyFrom(T[] array) => Copy(array, this);

        [WriteAccessRequired]
        public void CopyFrom(UnsafeArray<T> array) => Copy(array, this);

        public void CopyTo(T[] array) => Copy(this, array);

        public void CopyTo(UnsafeArray<T> array) => Copy(this, array);

        public TU ReinterpretLoad<TU>(int sourceIndex)
            where TU : unmanaged
        {
            this.CheckReinterpretLoadRange<TU>(sourceIndex);

            var offset = UnsafeUtility.SizeOf<T>() * (long)sourceIndex;
            var startBuffer = new IntPtr(((IntPtr)this.buffer).ToInt64() + offset);
            return UnsafeUtility.ReadArrayElement<TU>((void*)startBuffer, 0);
        }

        public void ReinterpretStore<TU>(int destIndex, TU data)
            where TU : unmanaged
        {
            this.CheckReinterpretStoreRange<TU>(destIndex);

            var offset = UnsafeUtility.SizeOf<T>() * (long)destIndex;
            var startBuffer = new IntPtr(((IntPtr)this.buffer).ToInt64() + offset);

            UnsafeUtility.WriteArrayElement((void*)startBuffer, 0, data);
        }

        public UnsafeArray<TU> Reinterpret<TU>()
            where TU : unmanaged
        {
            CheckReinterpretSize<TU>();
            return this.InternalReinterpret<TU>(this.Length);
        }

        [Pure]
        public UnsafeArray<TU> Reinterpret<TU>(int expectedTypeSize)
            where TU : unmanaged
        {
            long tSize = UnsafeUtility.SizeOf<T>();
            long uSize = UnsafeUtility.SizeOf<TU>();
            var byteLen = this.Length * tSize;
            var uLen = byteLen / uSize;
            this.CheckReinterpretSize<TU>(tSize, uSize, expectedTypeSize, byteLen, uLen);
            return this.InternalReinterpret<TU>((int)uLen);
        }

        public UnsafeArray<T> GetSubArray(int start, int newLength)
        {
            this.CheckGetSubArrayArguments(start, newLength);

            var offset = UnsafeUtility.SizeOf<T>() * (long)start;
            var startBuffer = new IntPtr(((IntPtr)this.buffer).ToInt64() + offset);
            return new UnsafeArray<T>((void*)startBuffer, newLength, Allocator.Invalid);
        }

        public UnsafeArray<T>.ReadOnly AsReadOnly() => new(this.buffer, this.length);

        public T[] ToArray()
        {
            var dst = new T[this.Length];
            Copy(this, dst, this.Length);
            return dst;
        }

        public void* GetUnsafePtr()
        {
            return this.buffer;
        }

        public UnsafeArray<T>.Enumerator GetEnumerator() => new(ref this);

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => new UnsafeArray<T>.Enumerator(ref this);

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        public bool Equals(UnsafeArray<T> other) => this.buffer == other.buffer && this.length == other.length;

        public override bool Equals(object obj) => obj != null && (obj is UnsafeArray<T> other && this.Equals(other));

        public override int GetHashCode() => (int)this.buffer * 397 ^ this.length;

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckAllocateArguments(int length, Allocator allocator)
        {
            if (allocator <= Allocator.None)
            {
                throw new ArgumentException("Allocator must be Temp, TempJob or Persistent", nameof(allocator));
            }

            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length), "Length must be >= 0");
            }
        }

        private static void Allocate(int length, Allocator allocator, out UnsafeArray<T> array)
        {
            var num = UnsafeUtility.SizeOf<T>() * (long)length;
            CheckAllocateArguments(length, allocator);
            array = default;
            array.buffer = UnsafeUtility.Malloc(num, UnsafeUtility.AlignOf<T>(), allocator);
            array.length = length;
            array.allocatorLabel = allocator;
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
                throw new ArgumentException(
                    "length is greater than the number of elements from srcIndex to the end of the source UnsafeArray.", nameof(length));
            }

            if (srcIndex + length < 0)
            {
                throw new ArgumentException("srcIndex + length causes an integer overflow");
            }

            if (dstIndex + length > dstLength)
            {
                throw new ArgumentException(
                    "length is greater than the number of elements from dstIndex to the end of the destination UnsafeArray.", nameof(length));
            }

            if (dstIndex + length < 0)
            {
                throw new ArgumentException("dstIndex + length causes an integer overflow");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckReinterpretSize<TU>()
            where TU : unmanaged
        {
            if (UnsafeUtility.SizeOf<T>() != UnsafeUtility.SizeOf<TU>())
            {
                throw new InvalidOperationException(
                    $"Types {typeof(T)} and {typeof(TU)} are different sizes - direct reinterpretation is not possible. If this is what you intended, use Reinterpret(<type size>)");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckConvertArguments(int length)
        {
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length), "Length must be >= 0");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void CheckReinterpretLoadRange<TU>(int sourceIndex)
            where TU : unmanaged
        {
            long num1 = UnsafeUtility.SizeOf<T>();
            long num2 = UnsafeUtility.SizeOf<TU>();
            var num3 = this.Length * num1;
            var num4 = sourceIndex * num1;
            var num5 = num4 + num2;
            if (num4 < 0L || num5 > num3)
            {
                throw new ArgumentOutOfRangeException(nameof(sourceIndex), "loaded byte range must fall inside container bounds");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void CheckReinterpretStoreRange<TU>(int destIndex)
            where TU : unmanaged
        {
            long num1 = UnsafeUtility.SizeOf<T>();
            long num2 = UnsafeUtility.SizeOf<TU>();
            var num3 = this.Length * num1;
            var num4 = destIndex * num1;
            var num5 = num4 + num2;
            if (num4 < 0L || num5 > num3)
            {
                throw new ArgumentOutOfRangeException(nameof(destIndex), "stored byte range must fall inside container bounds");
            }
        }

        private UnsafeArray<TU> InternalReinterpret<TU>(int newLength)
            where TU : unmanaged
        {
            return new UnsafeArray<TU>(this.buffer, newLength, this.allocatorLabel);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void CheckReinterpretSize<TU>(long tSize, long uSize, int expectedTypeSize, long byteLen, long uLen)
        {
            if (tSize != expectedTypeSize)
            {
                throw new InvalidOperationException($"Type {typeof(T)} was expected to be {expectedTypeSize} but is {tSize} bytes");
            }

            if (uLen * uSize != byteLen)
            {
                throw new InvalidOperationException(
                    $"Types {typeof(T)} (array length {this.Length}) and {typeof(TU)} cannot be aliased due to size constraints. The size of the types and lengths involved must line up.");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void CheckIndexRange(int index)
        {
            if (index < 0 || index >= this.length)
            {
                throw new IndexOutOfRangeException($"Index {(object)index} is out of range of '{(object)this.Length}' Length.");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void CheckGetSubArrayArguments(int start, int subArrayLength)
        {
            if (start < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(start), "start must be >= 0");
            }

            if (start + subArrayLength > this.Length)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(subArrayLength),
                    $"sub array range {start}-{start + subArrayLength - 1} is outside the range of the native array 0-{this.Length - 1}");
            }

            if (start + subArrayLength < 0)
            {
                throw new ArgumentException(
                    $"sub array range {start}-{start + subArrayLength - 1} caused an integer overflow and is outside the range of the native array 0-{this.Length - 1}");
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

            public void Reset() => this.index = -1;
        }

        /// <summary>
        ///     <para>UnsafeArray interface constrained to read-only operation.</para>
        /// </summary>
        [DebuggerTypeProxy(typeof(UnsafeArrayReadOnlyDebugView<>))]
        [NativeContainerIsReadOnly]
        [DebuggerDisplay("Length = {Length}")]
        [NativeContainer]
        public struct ReadOnly
        {
            [NativeDisableUnsafePtrRestriction]
            private readonly void* buffer;

            private readonly int length;

            internal ReadOnly(void* buffer, int length)
            {
                this.buffer = buffer;
                this.length = length;
            }

            public int Length => this.length;

            internal void* Buffer => this.buffer;

            public T this[int index]
            {
                get
                {
                    this.CheckElementReadAccess(index);
                    return UnsafeUtility.ReadArrayElement<T>(this.buffer, index);
                }
            }

            public void CopyTo(T[] array) => Copy(this, array);

            public void CopyTo(UnsafeArray<T> array) => Copy(this, array);

            public T[] ToArray()
            {
                var dst = new T[this.length];
                Copy(this, dst, this.length);
                return dst;
            }

            public UnsafeArray<TU>.ReadOnly Reinterpret<TU>()
                where TU : unmanaged
            {
                CheckReinterpretSize<TU>();
                return new UnsafeArray<TU>.ReadOnly(this.buffer, this.length);
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            private void CheckElementReadAccess(int index)
            {
                if (index < 0 || index >= this.length)
                {
                    throw new IndexOutOfRangeException($"Index {index} is out of range (must be between 0 and {this.length - 1}).");
                }
            }
        }
    }

    internal struct UnsafeArrayDisposeJob : IJob
    {
        internal UnsafeArrayDispose Data;

        public void Execute() => this.Data.Dispose();
    }

    [NativeContainer]
    internal struct UnsafeArrayDispose
    {
        [NativeDisableUnsafePtrRestriction]
        internal unsafe void* Buffer;

        internal Allocator AllocatorLabel;

        public unsafe void Dispose() => UnsafeUtility.Free(this.Buffer, this.AllocatorLabel);
    }

    internal sealed class UnsafeArrayDebugView<T>
        where T : unmanaged
    {
        private UnsafeArray<T> array;

        public UnsafeArrayDebugView(UnsafeArray<T> array) => this.array = array;

        public T[] Items => this.array.ToArray();
    }

    internal sealed class UnsafeArrayReadOnlyDebugView<T>
        where T : unmanaged
    {
        private UnsafeArray<T>.ReadOnly array;

        public UnsafeArrayReadOnlyDebugView(UnsafeArray<T>.ReadOnly array) => this.array = array;

        public T[] Items => this.array.ToArray();
    }
}
