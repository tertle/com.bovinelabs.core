namespace BovineLabs.Core.Collections
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using BovineLabs.Core.Internal;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
    using Unity.Mathematics;

    [StructLayout(LayoutKind.Sequential)]
    [DebuggerDisplay("Length = {Length}, Capacity = {Capacity}, IsCreated = {IsCreated}")]
    [DebuggerTypeProxy(typeof(DynamicBufferDebugView<>))]
    public unsafe struct UnsafeDynamicBuffer<T> : IQueryTypeParameter, IEnumerable<T>, INativeList<T>
        where T : unmanaged, IBufferElementData
    {
        [NoAlias]
        private BufferHeader* buffer;

        // Stores original internal capacity of the buffer header, so heap excess can be removed entirely when trimming.
        private int internalCapacity;

        internal UnsafeDynamicBuffer(BufferHeader* header, int internalCapacity)
        {
            this.buffer = header;
            this.internalCapacity = internalCapacity;
        }

        public int Length
        {
            readonly get => this.buffer->Length;
            set => this.ResizeUninitialized(value);
        }

        public int Capacity
        {
            readonly get
            {
                return this.buffer->Capacity;
            }

            set
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
                if (value < this.Length)
                {
                    throw new InvalidOperationException($"Capacity {value} can't be set smaller than Length {this.Length}");
                }
#endif
                BufferHeader.SetCapacity(this.buffer, value, UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(), BufferHeader.TrashMode.RetainOldData, false,
                    0, this.internalCapacity);
            }
        }

        public readonly bool IsEmpty => !this.IsCreated || this.Length == 0;

        public readonly bool IsCreated => this.buffer != null;

        public T this[int index]
        {
            readonly get
            {
                this.CheckBounds(index);
                return UnsafeUtility.ReadArrayElement<T>(BufferHeader.GetElementPointer(this.buffer), index);
            }

            set
            {
                this.CheckBounds(index);
                UnsafeUtility.WriteArrayElement(BufferHeader.GetElementPointer(this.buffer), index, value);
            }
        }

        public ref T ElementAt(int index)
        {
            this.CheckBounds(index);
            return ref UnsafeUtility.ArrayElementAsRef<T>(BufferHeader.GetElementPointer(this.buffer), index);
        }

        public void ResizeUninitialized(int length)
        {
            this.EnsureCapacity(length);
            this.buffer->Length = length;
        }

        public void Resize(int length, NativeArrayOptions options)
        {
            this.EnsureCapacity(length);

            var oldLength = this.buffer->Length;
            this.buffer->Length = length;
            if (options == NativeArrayOptions.ClearMemory && oldLength < length)
            {
                var num = length - oldLength;
                var ptr = BufferHeader.GetElementPointer(this.buffer);
                var sizeOf = UnsafeUtility.SizeOf<T>();
                UnsafeUtility.MemClear(ptr + (oldLength * sizeOf), num * sizeOf);
            }
        }

        public void EnsureCapacity(int length)
        {
            BufferHeader.EnsureCapacity(this.buffer, length, UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(), BufferHeader.TrashMode.RetainOldData, false,
                0);
        }

        /// <summary>
        /// Does not overwrite the cleared memory or shrink capacity.
        /// </summary>
        public void Clear()
        {
            this.buffer->Length = 0;
        }

        public void TrimExcess()
        {
            var oldPtr = this.buffer->Pointer;
            var length = this.buffer->Length;

            if (length == this.Capacity || oldPtr == null)
            {
                return;
            }

            var elemSize = UnsafeUtility.SizeOf<T>();
            var elemAlign = UnsafeUtility.AlignOf<T>();

            bool isInternal;
            byte* newPtr;

            // If the size fits in the internal buffer, prefer to move the elements back there.
            if (length <= this.internalCapacity)
            {
                newPtr = (byte*)(this.buffer + 1);
                isInternal = true;
            }
            else
            {
                newPtr = (byte*)CollectionMemory.Allocate((long)elemSize * length, elemAlign, Allocator.Persistent);
                isInternal = false;
            }

            UnsafeUtility.MemCpy(newPtr, oldPtr, (long)elemSize * length);

            this.buffer->Capacity = Math.Max(length, this.internalCapacity);
            this.buffer->Pointer = isInternal ? null : newPtr;

            CollectionMemory.Free(oldPtr, Allocator.Persistent);
        }

        public int Add(T elem)
        {
            var length = this.Length;
            this.ResizeUninitialized(length + 1);
            this[length] = elem;
            return length;
        }

        public void Insert(int index, T elem)
        {
            var length = this.Length;
            this.ResizeUninitialized(length + 1);
            this.CheckBounds(index); // CheckBounds after ResizeUninitialized since index == length is allowed
            var elemSize = UnsafeUtility.SizeOf<T>();
            var basePtr = BufferHeader.GetElementPointer(this.buffer);
            UnsafeUtility.MemMove(basePtr + ((index + 1) * elemSize), basePtr + (index * elemSize), (long)elemSize * (length - index));
            this[index] = elem;
        }

        public void AddRange(NativeArray<T> newElems)
        {
            var elemSize = UnsafeUtility.SizeOf<T>();
            var oldLength = this.Length;
            this.ResizeUninitialized(oldLength + newElems.Length);

            var basePtr = BufferHeader.GetElementPointer(this.buffer);
            UnsafeUtility.MemCpy(basePtr + ((long)oldLength * elemSize), newElems.GetUnsafeReadOnlyPtr(), (long)elemSize * newElems.Length);
        }

        public void RemoveRange(int index, int count)
        {
            this.CheckBounds(index);
            if (count == 0)
            {
                return;
            }

            this.CheckBounds((index + count) - 1);

            var elemSize = UnsafeUtility.SizeOf<T>();
            var basePtr = BufferHeader.GetElementPointer(this.buffer);

            UnsafeUtility.MemMove(basePtr + (index * elemSize), basePtr + ((index + count) * elemSize), (long)elemSize * (this.Length - count - index));

            this.buffer->Length -= count;
        }

        public void RemoveRangeSwapBack(int index, int count)
        {
            this.CheckBounds(index);
            if (count == 0)
            {
                return;
            }

            this.CheckBounds((index + count) - 1);

            ref var l = ref this.buffer->Length;
            var basePtr = BufferHeader.GetElementPointer(this.buffer);
            var elemSize = UnsafeUtility.SizeOf<T>();
            var copyFrom = math.max(l - count, index + count);
            void* dst = basePtr + (index * elemSize);
            void* src = basePtr + (copyFrom * elemSize);
            UnsafeUtility.MemMove(dst, src, (l - copyFrom) * elemSize);
            l -= count;
        }

        public void RemoveAt(int index)
        {
            this.RemoveRange(index, 1);
        }

        public void RemoveAtSwapBack(int index)
        {
            this.CheckBounds(index);

            ref var l = ref this.buffer->Length;
            l -= 1;
            var newLength = l;
            if (index != newLength)
            {
                var basePtr = BufferHeader.GetElementPointer(this.buffer);
                UnsafeUtility.WriteArrayElement(basePtr, index, UnsafeUtility.ReadArrayElement<T>(basePtr, newLength));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void* GetUnsafePtr()
        {
            return BufferHeader.GetElementPointer(this.buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void* GetUnsafeReadOnlyPtr()
        {
            return BufferHeader.GetElementPointer(this.buffer);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        private static void AssertReinterpretSizesMatch<TU>()
            where TU : struct
        {
            if (UnsafeUtility.SizeOf<TU>() != UnsafeUtility.SizeOf<T>())
            {
                throw new InvalidOperationException($"Types {typeof(TU)} and {typeof(T)} are of different sizes; cannot reinterpret");
            }
        }

        /// <summary>
        /// Aliases the same buffer memory; both element types must have the same size.
        /// </summary>
        public readonly UnsafeDynamicBuffer<TU> Reinterpret<TU>()
            where TU : unmanaged, IBufferElementData
        {
            AssertReinterpretSizesMatch<TU>();
            return new UnsafeDynamicBuffer<TU>(this.buffer, this.internalCapacity);
        }

        /// <summary>
        /// Aliases buffer memory and becomes invalid on reallocation, including from Add or TrimExcess.
        /// </summary>
        public readonly NativeArray<T> AsNativeArray()
        {
            var shadow = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(BufferHeader.GetElementPointer(this.buffer), this.Length, Allocator.None);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref shadow, AtomicSafetyHandle.GetTempMemoryHandle());
#endif
            return shadow;
        }

        public readonly NativeArray<T>.Enumerator GetEnumerator()
        {
            var array = this.AsNativeArray();
            return new NativeArray<T>.Enumerator(ref array);
        }

        readonly IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        readonly IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public readonly NativeArray<T> ToNativeArray(AllocatorManager.AllocatorHandle allocator)
        {
            return CollectionHelper.CreateNativeArray(this.AsNativeArray(), allocator);
        }

        public void CopyFrom(NativeArray<T> v)
        {
            //todo remove workaround: See DOTS-1454
            this.ResizeUninitialized(v.Length);
            var vs = new NativeSlice<T>(v);
            vs.CopyTo(this.AsNativeArray());
        }

        public void CopyFrom(NativeSlice<T> v)
        {
            this.ResizeUninitialized(v.Length);
            v.CopyTo(this.AsNativeArray());
        }

        public void CopyFrom(UnsafeDynamicBuffer<T> v)
        {
            this.ResizeUninitialized(v.Length);

            UnsafeUtility.MemCpy(BufferHeader.GetElementPointer(this.buffer), BufferHeader.GetElementPointer(v.buffer),
                this.Length * UnsafeUtility.SizeOf<T>());
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        private readonly void CheckBounds(int index)
        {
            if ((uint)index >= (uint)this.Length)
            {
                throw new IndexOutOfRangeException($"Index {index} is out of range in DynamicBuffer of '{this.Length}' Length.");
            }
        }
    }

    internal sealed class DynamicBufferDebugView<T>
        where T : unmanaged, IBufferElementData
    {
        private readonly UnsafeDynamicBuffer<T> buffer;

        public DynamicBufferDebugView(UnsafeDynamicBuffer<T> source)
        {
            this.buffer = source;
        }

        public T[] Items => this.buffer.AsNativeArray().ToArray();
    }
}
