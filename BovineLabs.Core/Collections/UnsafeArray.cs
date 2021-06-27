namespace BovineLabs.Core.Collections
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs;
    using UnityEngine.Internal;

    /// <summary>
    /// A UnsafeArray exposes a buffer of native memory to managed code, making it possible to share data between managed and native without marshalling costs.
    /// </summary>
    [NativeContainer]
    [NativeContainerSupportsDeallocateOnJobCompletion]
    [NativeContainerSupportsMinMaxWriteRestriction]
    [DebuggerDisplay("Length = {" + nameof(Length) + "}")]
    [DebuggerTypeProxy(typeof(UnsafeArrayDebugView<>))]
    // [NativeContainerSupportsDeferredConvertListToArray]
    public struct UnsafeArray<T> : IDisposable, IEnumerable<T>, IEquatable<UnsafeArray<T>>
        where T : struct
    {
        [NativeDisableUnsafePtrRestriction]
        internal unsafe void* m_Buffer;

        internal int m_Length;
        internal int m_MinIndex;
        internal int m_MaxIndex;
        internal Allocator m_AllocatorLabel;

        public unsafe UnsafeArray(int length, Allocator allocator, NativeArrayOptions options = NativeArrayOptions.ClearMemory)
        {
            Allocate(length, allocator, out this);
            if ((options & NativeArrayOptions.ClearMemory) != NativeArrayOptions.ClearMemory)
            {
                return;
            }

            UnsafeUtility.MemClear(this.m_Buffer, UnsafeUtility.SizeOf<T>() * (long)this.Length);
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

        public int Length => this.m_Length;

        public unsafe bool IsCreated => (IntPtr)this.m_Buffer != IntPtr.Zero;

        public unsafe T this[int index]
        {
            get
            {
                this.CheckElementReadAccess(index);
                return UnsafeUtility.ReadArrayElement<T>(this.m_Buffer, index);
            }

            [WriteAccessRequired]
            set
            {
                this.CheckElementWriteAccess(index);
                UnsafeUtility.WriteArrayElement(this.m_Buffer, index, value);
            }
        }

        public static bool operator ==(UnsafeArray<T> left, UnsafeArray<T> right) => left.Equals(right);

        public static bool operator !=(UnsafeArray<T> left, UnsafeArray<T> right) => !left.Equals(right);

        [WriteAccessRequired]
        public unsafe void Dispose()
        {
            if ((IntPtr)this.m_Buffer == IntPtr.Zero)
            {
                throw new ObjectDisposedException("The UnsafeArray is already disposed.");
            }

            if (this.m_AllocatorLabel == Allocator.Invalid)
            {
                throw new InvalidOperationException("The UnsafeArray can not be Disposed because it was not allocated with a valid allocator.");
            }

            if (this.m_AllocatorLabel > Allocator.None)
            {
                UnsafeUtility.Free(this.m_Buffer, this.m_AllocatorLabel);
                this.m_AllocatorLabel = Allocator.Invalid;
            }

            this.m_Buffer = null;
            this.m_Length = 0;
        }

        public unsafe JobHandle Dispose(JobHandle inputDeps)
        {
            if (this.m_AllocatorLabel == Allocator.Invalid)
            {
                throw new InvalidOperationException("The UnsafeArray can not be Disposed because it was not allocated with a valid allocator.");
            }

            if ((IntPtr)this.m_Buffer == IntPtr.Zero)
            {
                throw new InvalidOperationException("The UnsafeArray is already disposed.");
            }

            if (this.m_AllocatorLabel > Allocator.None)
            {
                var jobHandle = new UnsafeArrayDisposeJob()
                {
                    Data = new UnsafeArrayDispose
                    {
                        m_Buffer = this.m_Buffer,
                        m_AllocatorLabel = this.m_AllocatorLabel,
                    },
                }.Schedule(inputDeps);

                this.m_Buffer = null;
                this.m_Length = 0;
                this.m_AllocatorLabel = Allocator.Invalid;
                return jobHandle;
            }

            this.m_Buffer = null;
            this.m_Length = 0;
            return inputDeps;
        }

        [WriteAccessRequired]
        public void CopyFrom(T[] array) => Copy(array, this);

        [WriteAccessRequired]
        public void CopyFrom(UnsafeArray<T> array) => Copy(array, this);

        public void CopyTo(T[] array) => Copy(this, array);

        public void CopyTo(UnsafeArray<T> array) => Copy(this, array);

        public T[] ToArray()
        {
            var dst = new T[this.Length];
            Copy(this, dst, this.Length);
            return dst;
        }

        public UnsafeArray<T>.Enumerator GetEnumerator() => new UnsafeArray<T>.Enumerator(ref this);

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => new UnsafeArray<T>.Enumerator(ref this);

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        public unsafe bool Equals(UnsafeArray<T> other) => this.m_Buffer == other.m_Buffer && this.m_Length == other.m_Length;

        public override bool Equals(object obj) => obj != null && (obj is UnsafeArray<T> other && this.Equals(other));

        public override unsafe int GetHashCode() => (int)this.m_Buffer * 397 ^ this.m_Length;

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckAllocateArguments(int length, Allocator allocator, long totalSize)
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

        private static unsafe void Allocate(int length, Allocator allocator, out UnsafeArray<T> array)
        {
            var num = UnsafeUtility.SizeOf<T>() * (long)length;
            CheckAllocateArguments(length, allocator, num);
            array = default;
            array.m_Buffer = UnsafeUtility.Malloc(num, UnsafeUtility.AlignOf<T>(), allocator);
            array.m_Length = length;
            array.m_AllocatorLabel = allocator;
            array.m_MinIndex = 0;
            array.m_MaxIndex = length - 1;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckCopyLengths(int srcLength, int dstLength)
        {
            if (srcLength != dstLength)
            {
                throw new ArgumentException("source and destination length must be the same");
            }
        }

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
                throw new ArgumentException("length is greater than the number of elements from srcIndex to the end of the source UnsafeArray.", nameof(length));
            }

            if (srcIndex + length < 0)
            {
                throw new ArgumentException("srcIndex + length causes an integer overflow");
            }

            if (dstIndex + length > dstLength)
            {
                throw new ArgumentException("length is greater than the number of elements from dstIndex to the end of the destination UnsafeArray.", nameof(length));
            }

            if (dstIndex + length < 0)
            {
                throw new ArgumentException("dstIndex + length causes an integer overflow");
            }
        }

        public static unsafe void Copy(UnsafeArray<T> src, int srcIndex, UnsafeArray<T> dst, int dstIndex, int length)
        {
            CheckCopyArguments(src.Length, srcIndex, dst.Length, dstIndex, length);
            UnsafeUtility.MemCpy(
                (void*)((IntPtr)dst.m_Buffer + (dstIndex * UnsafeUtility.SizeOf<T>())),
                (void*)((IntPtr)src.m_Buffer + (srcIndex * UnsafeUtility.SizeOf<T>())),
                UnsafeUtility.SizeOf<T>() * length);
        }

        public static unsafe void Copy(UnsafeArray<T>.ReadOnly src, int srcIndex, UnsafeArray<T> dst, int dstIndex, int length)
        {
            CheckCopyArguments(src.Length, srcIndex, dst.Length, dstIndex, length);
            UnsafeUtility.MemCpy(
                (void*)((IntPtr)dst.m_Buffer + (dstIndex * UnsafeUtility.SizeOf<T>())),
                (void*)((IntPtr)src.m_Buffer + (srcIndex * UnsafeUtility.SizeOf<T>())),
                UnsafeUtility.SizeOf<T>() * length);
        }

        public static unsafe void Copy(T[] src, int srcIndex, UnsafeArray<T> dst, int dstIndex, int length)
        {
            if (src == null)
            {
                throw new ArgumentNullException(nameof(src));
            }

            CheckCopyArguments(src.Length, srcIndex, dst.Length, dstIndex, length);
            var gcHandle = GCHandle.Alloc(src, GCHandleType.Pinned);
            var num = gcHandle.AddrOfPinnedObject();
            UnsafeUtility.MemCpy(
                (void*)((IntPtr)dst.m_Buffer + (dstIndex * UnsafeUtility.SizeOf<T>())),
                (void*)((IntPtr)(void*)num + (srcIndex * UnsafeUtility.SizeOf<T>())),
                UnsafeUtility.SizeOf<T>() * length);
            gcHandle.Free();
        }

        public static unsafe void Copy(UnsafeArray<T> src, int srcIndex, T[] dst, int dstIndex, int length)
        {
            if (dst == null)
            {
                throw new ArgumentNullException(nameof(dst));
            }

            CheckCopyArguments(src.Length, srcIndex, dst.Length, dstIndex, length);
            var gcHandle = GCHandle.Alloc(dst, GCHandleType.Pinned);
            UnsafeUtility.MemCpy(
                (void*)((IntPtr)(void*)gcHandle.AddrOfPinnedObject() + (dstIndex * UnsafeUtility.SizeOf<T>())),
                (void*)((IntPtr)src.m_Buffer + (srcIndex * UnsafeUtility.SizeOf<T>())),
                UnsafeUtility.SizeOf<T>() * length);
            gcHandle.Free();
        }

        public static unsafe void Copy(UnsafeArray<T>.ReadOnly src, int srcIndex, T[] dst, int dstIndex, int length)
        {
            if (dst == null)
            {
                throw new ArgumentNullException(nameof(dst));
            }

            CheckCopyArguments(src.Length, srcIndex, dst.Length, dstIndex, length);
            var gcHandle = GCHandle.Alloc(dst, GCHandleType.Pinned);
            UnsafeUtility.MemCpy(
                (void*)((IntPtr)(void*)gcHandle.AddrOfPinnedObject() + (dstIndex * UnsafeUtility.SizeOf<T>())),
                (void*)((IntPtr)src.m_Buffer + (srcIndex * UnsafeUtility.SizeOf<T>())),
                length * UnsafeUtility.SizeOf<T>());
            gcHandle.Free();
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void CheckReinterpretLoadRange<U>(int sourceIndex)
            where U : unmanaged
        {
            long num1 = UnsafeUtility.SizeOf<T>();
            long num2 = UnsafeUtility.SizeOf<U>();
            var num3 = this.Length * num1;
            var num4 = sourceIndex * num1;
            var num5 = num4 + num2;
            if (num4 < 0L || num5 > num3)
            {
                throw new ArgumentOutOfRangeException(nameof(sourceIndex), "loaded byte range must fall inside container bounds");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void CheckReinterpretStoreRange<U>(int destIndex)
            where U : unmanaged
        {
            long num1 = UnsafeUtility.SizeOf<T>();
            long num2 = UnsafeUtility.SizeOf<U>();
            var num3 = this.Length * num1;
            var num4 = destIndex * num1;
            var num5 = num4 + num2;
            if (num4 < 0L || num5 > num3)
            {
                throw new ArgumentOutOfRangeException(nameof(destIndex), "stored byte range must fall inside container bounds");
            }
        }

        public unsafe U ReinterpretLoad<U>(int sourceIndex)
            where U : unmanaged
        {
            this.CheckReinterpretLoadRange<U>(sourceIndex);

            var offset = UnsafeUtility.SizeOf<T>() * (long)sourceIndex;
            var startBuffer = new IntPtr(((IntPtr)this.m_Buffer).ToInt64() + offset);
            return UnsafeUtility.ReadArrayElement<U>((void*)startBuffer, 0);
        }

        public unsafe void ReinterpretStore<U>(int destIndex, U data)
            where U : unmanaged
        {
            this.CheckReinterpretStoreRange<U>(destIndex);

            var offset = UnsafeUtility.SizeOf<T>() * (long)destIndex;
            var startBuffer = new IntPtr(((IntPtr)this.m_Buffer).ToInt64() + offset);

            UnsafeUtility.WriteArrayElement((void*)startBuffer, 0, data);
        }

        private unsafe UnsafeArray<U> InternalReinterpret<U>(int length)
            where U : unmanaged
        {
            return UnsafeArrayUtility.ConvertExistingDataToUnsafeArray<U>(this.m_Buffer, length, this.m_AllocatorLabel);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckReinterpretSize<U>()
            where U : unmanaged
        {
            if (UnsafeUtility.SizeOf<T>() != UnsafeUtility.SizeOf<U>())
            {
                throw new InvalidOperationException(
                    $"Types {typeof(T)} and {typeof(U)} are different sizes - direct reinterpretation is not possible. If this is what you intended, use Reinterpret(<type size>)");
            }
        }

        public UnsafeArray<U> Reinterpret<U>()
            where U : unmanaged
        {
            CheckReinterpretSize<U>();
            return this.InternalReinterpret<U>(this.Length);
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

        public UnsafeArray<U> Reinterpret<U>(int expectedTypeSize)
            where U : unmanaged
        {
            long tSize = UnsafeUtility.SizeOf<T>();
            long uSize = UnsafeUtility.SizeOf<U>();
            var byteLen = this.Length * tSize;
            var uLen = byteLen / uSize;
            this.CheckReinterpretSize<U>(tSize, uSize, expectedTypeSize, byteLen, uLen);
            return this.InternalReinterpret<U>((int)uLen);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void CheckElementReadAccess(int index)
        {
            if (index < this.m_MinIndex || index > this.m_MaxIndex)
            {
                this.FailOutOfRangeError(index);
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void CheckElementWriteAccess(int index)
        {
            if (index < this.m_MinIndex || index > this.m_MaxIndex)
            {
                this.FailOutOfRangeError(index);
            }
        }


        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void FailOutOfRangeError(int index)
        {
            if (index < this.Length && (this.m_MinIndex != 0 || this.m_MaxIndex != this.Length - 1))
            {
                throw new IndexOutOfRangeException(
                    $"Index {(object)index} is out of restricted IJobParallelFor range [{this.m_MinIndex}...{this.m_MaxIndex}] in ReadWriteBuffer.\n" +
                    "ReadWriteBuffers are restricted to only read & write the element at the job index. You can use double buffering strategies to avoid race conditions due to reading & writing in parallel to the same elements from a job.");
            }

            throw new IndexOutOfRangeException($"Index {(object)index} is out of range of '{(object)this.Length}' Length.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void CheckGetSubArrayArguments(int start, int length)
        {
            if (start < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(start), "start must be >= 0");
            }

            if (start + length > this.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(length),
                    $"sub array range {start}-{start + length - 1} is outside the range of the native array 0-{this.Length - 1}");
            }

            if (start + length < 0)
            {
                throw new ArgumentException(
                    $"sub array range {start}-{start + length - 1} caused an integer overflow and is outside the range of the native array 0-{this.Length - 1}");
            }
        }

        public unsafe UnsafeArray<T> GetSubArray(int start, int length)
        {
            this.CheckGetSubArrayArguments(start, length);

            var offset = UnsafeUtility.SizeOf<T>() * (long)start;
            var startBuffer = new IntPtr(((IntPtr)this.m_Buffer).ToInt64() + offset);
            return UnsafeArrayUtility.ConvertExistingDataToUnsafeArray<T>((void*)startBuffer, length, Allocator.Invalid);
        }

        public unsafe UnsafeArray<T>.ReadOnly AsReadOnly() => new UnsafeArray<T>.ReadOnly(this.m_Buffer, this.m_Length);

        [ExcludeFromDocs]
        public struct Enumerator : IEnumerator<T>
        {
            private UnsafeArray<T> m_Array;
            private int m_Index;

            public Enumerator(ref UnsafeArray<T> array)
            {
                this.m_Array = array;
                this.m_Index = -1;
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                ++this.m_Index;
                return this.m_Index < this.m_Array.Length;
            }

            public void Reset() => this.m_Index = -1;

            public T Current => this.m_Array[this.m_Index];

            object IEnumerator.Current => this.Current;
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
            internal unsafe void* m_Buffer;

            internal int m_Length;

            internal unsafe ReadOnly(void* buffer, int length)
            {
                this.m_Buffer = buffer;
                this.m_Length = length;
            }

            public int Length => this.m_Length;

            public void CopyTo(T[] array) => Copy(this, array);

            public void CopyTo(UnsafeArray<T> array) => Copy(this, array);

            public T[] ToArray()
            {
                var dst = new T[this.m_Length];
                Copy(this, dst, this.m_Length);
                return dst;
            }

            public unsafe UnsafeArray<U>.ReadOnly Reinterpret<U>()
                where U : unmanaged
            {
                CheckReinterpretSize<U>();
                return new UnsafeArray<U>.ReadOnly(this.m_Buffer, this.m_Length);
            }

            public unsafe T this[int index]
            {
                get
                {
                    this.CheckElementReadAccess(index);
                    return UnsafeUtility.ReadArrayElement<T>(this.m_Buffer, index);
                }
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            private void CheckElementReadAccess(int index)
            {
                if (index < 0 || index >= this.m_Length)
                {
                    throw new IndexOutOfRangeException($"Index {index} is out of range (must be between 0 and {this.m_Length - 1}).");
                }
            }
        }
    }

    internal sealed class UnsafeArrayDebugView<T>
        where T : unmanaged
    {
        private UnsafeArray<T> m_Array;

        public UnsafeArrayDebugView(UnsafeArray<T> array) => this.m_Array = array;

        public T[] Items => this.m_Array.ToArray();
    }

    internal sealed class UnsafeArrayReadOnlyDebugView<T>
        where T : unmanaged
    {
        private UnsafeArray<T>.ReadOnly m_Array;

        public UnsafeArrayReadOnlyDebugView(UnsafeArray<T>.ReadOnly array) => this.m_Array = array;

        public T[] Items => this.m_Array.ToArray();
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
        internal unsafe void* m_Buffer;
        internal Allocator m_AllocatorLabel;

        public unsafe void Dispose() => UnsafeUtility.Free(this.m_Buffer, this.m_AllocatorLabel);
    }
}