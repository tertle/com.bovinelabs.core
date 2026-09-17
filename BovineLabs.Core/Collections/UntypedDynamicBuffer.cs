namespace BovineLabs.Core.Collections
{
    using System;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    [StructLayout(LayoutKind.Sequential)]
    [NativeContainer]
    [DebuggerDisplay("Length = {Length}, Capacity = {Capacity}, IsCreated = {IsCreated}")]
    public unsafe struct UntypedDynamicBuffer
    {
        public const int AlignOf = 4;

        [NativeDisableUnsafePtrRestriction]
        [NoAlias]
        private readonly BufferHeader* buffer;

        // Stores original internal capacity of the buffer header, so heap excess can be removed entirely when trimming.
        private readonly int internalCapacity;

        private readonly int alignOf;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal AtomicSafetyHandle m_Safety0;
        internal AtomicSafetyHandle m_Safety1;
        internal int m_SafetyReadOnlyCount;
        internal int m_SafetyReadWriteCount;

        internal byte m_IsReadOnly;
        internal byte m_useMemoryInitPattern;
        internal byte m_memoryInitPattern;
#endif

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal UntypedDynamicBuffer(
            BufferHeader* header, AtomicSafetyHandle safety, AtomicSafetyHandle arrayInvalidationSafety, bool isReadOnly, bool useMemoryInitPattern,
            byte memoryInitPattern, int internalCapacity, int elementSize, int alignOf)
        {
            this.buffer = header;
            this.m_Safety0 = safety;
            this.m_Safety1 = arrayInvalidationSafety;
            this.m_SafetyReadOnlyCount = isReadOnly ? 2 : 0;
            this.m_SafetyReadWriteCount = isReadOnly ? 0 : 2;
            this.m_IsReadOnly = (byte)(isReadOnly ? 1 : 0);
            this.internalCapacity = internalCapacity;
            this.m_useMemoryInitPattern = (byte)(useMemoryInitPattern ? 1 : 0);
            this.m_memoryInitPattern = memoryInitPattern;
            this.ElementSize = elementSize;
            this.alignOf = alignOf;
        }

#else
        internal UntypedDynamicBuffer(BufferHeader* header, int internalCapacity, int elementSize, int alignOf)
        {
            this.buffer = header;
            this.internalCapacity = internalCapacity;
            this.ElementSize = elementSize;
            this.alignOf = alignOf;
        }
#endif

        public int Length
        {
            get
            {
                this.CheckReadAccess();
                return this.buffer->Length;
            }
            set => this.ResizeUninitialized(value);
        }

        /// <summary>
        /// Cannot be smaller than Length. Every capacity change may reallocate; small increments do not reserve extra growth space.
        /// </summary>
        public int Capacity
        {
            get
            {
                this.CheckReadAccess();
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
                this.CheckWriteAccessAndInvalidateArrayAliases();
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                BufferHeader.SetCapacity(this.buffer, value, this.ElementSize, this.alignOf, BufferHeader.TrashMode.RetainOldData,
                    this.m_useMemoryInitPattern == 1, this.m_memoryInitPattern, this.internalCapacity);
#else
                BufferHeader.SetCapacity(
                    this.buffer, value, this.ElementSize, this.alignOf, BufferHeader.TrashMode.RetainOldData, false, 0, this.internalCapacity);
#endif
            }
        }

        public bool IsEmpty => !this.IsCreated || this.Length == 0;

        public bool IsCreated => this.buffer != null;

        public int ElementSize { get; }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        private void CheckBounds(int index)
        {
            if ((uint)index >= (uint)this.Length)
            {
                throw new IndexOutOfRangeException($"Index {index} is out of range in DynamicBuffer of '{this.Length}' Length.");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void CheckReadAccess()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(this.m_Safety0);
            AtomicSafetyHandle.CheckReadAndThrow(this.m_Safety1);
#endif
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void CheckWriteAccess()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(this.m_Safety0);
            AtomicSafetyHandle.CheckWriteAndThrow(this.m_Safety1);
#endif
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void CheckWriteAccessAndInvalidateArrayAliases()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(this.m_Safety0);
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(this.m_Safety1);
#endif
        }

        public void* this[int index]
        {
            get
            {
                this.CheckReadAccess();
                this.CheckBounds(index);
                return BufferHeader.GetElementPointer(this.buffer) + (index * this.ElementSize);
            }

            set
            {
                this.CheckWriteAccess();
                this.CheckBounds(index);
                var dst = BufferHeader.GetElementPointer(this.buffer) + (index * this.ElementSize);
                UnsafeUtility.MemCpy(dst, value, this.ElementSize);
            }
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
                UnsafeUtility.MemClear(ptr + (oldLength * this.ElementSize), num * this.ElementSize);
            }
        }

        public void EnsureCapacity(int length)
        {
            this.CheckWriteAccessAndInvalidateArrayAliases();
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            BufferHeader.EnsureCapacity(this.buffer, length, this.ElementSize, this.alignOf, BufferHeader.TrashMode.RetainOldData,
                this.m_useMemoryInitPattern == 1, this.m_memoryInitPattern);
#else
            BufferHeader.EnsureCapacity(this.buffer, length, this.ElementSize, this.alignOf, BufferHeader.TrashMode.RetainOldData, false, 0);
#endif
        }

        /// <summary>
        /// Does not overwrite cleared memory or shrink capacity.
        /// </summary>
        public void Clear()
        {
            this.CheckWriteAccessAndInvalidateArrayAliases();

            this.buffer->Length = 0;
        }

        public int Add(void* elem)
        {
            this.CheckWriteAccess();
            var length = this.Length;
            this.ResizeUninitialized(length + 1);
            this[length] = elem;
            return length;
        }

        public void AddRange(void* elem, int count)
        {
            this.CheckWriteAccess();
            var oldLength = this.Length;
            this.ResizeUninitialized(oldLength + count);

            void* basePtr = BufferHeader.GetElementPointer(this.buffer) + (oldLength * this.ElementSize);
            UnsafeUtility.MemCpy(basePtr, elem, (long)this.ElementSize * count);
        }

        public void RemoveRange(int index, int count)
        {
            this.CheckWriteAccess();
            this.CheckBounds(index);
            if (count == 0)
            {
                return;
            }

            this.CheckBounds((index + count) - 1);

            var elemSize = this.ElementSize;
            var basePtr = BufferHeader.GetElementPointer(this.buffer);

            UnsafeUtility.MemMove(basePtr + (index * elemSize), basePtr + ((index + count) * elemSize), (long)elemSize * (this.Length - count - index));

            this.buffer->Length -= count;
        }

        public void RemoveAt(int index)
        {
            this.RemoveRange(index, 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void* GetUnsafePtr()
        {
            this.CheckWriteAccess();
            return BufferHeader.GetElementPointer(this.buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void* GetUnsafeReadOnlyPtr()
        {
            this.CheckReadAccess();
            return BufferHeader.GetElementPointer(this.buffer);
        }
    }
}
