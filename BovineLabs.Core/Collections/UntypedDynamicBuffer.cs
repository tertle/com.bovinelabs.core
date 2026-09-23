namespace BovineLabs.Core.Collections
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
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
        private readonly BufferHeader* _buffer;

        // Stores original internal capacity of the buffer header, so heap excess can be removed entirely when trimming.
        private readonly int _internalCapacity;

        private readonly int _alignOf;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Preserve Unity safety-handle field names.")]
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Preserve Unity safety-handle field names.")]
        internal AtomicSafetyHandle m_Safety0;
        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Preserve Unity safety-handle field names.")]
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Preserve Unity safety-handle field names.")]
        internal AtomicSafetyHandle m_Safety1;
        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Preserve Unity safety-handle field names.")]
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Preserve Unity safety-handle field names.")]
        internal int m_SafetyReadOnlyCount;
        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Preserve Unity safety-handle field names.")]
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Preserve Unity safety-handle field names.")]
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
            _buffer = header;
            m_Safety0 = safety;
            m_Safety1 = arrayInvalidationSafety;
            m_SafetyReadOnlyCount = isReadOnly ? 2 : 0;
            m_SafetyReadWriteCount = isReadOnly ? 0 : 2;
            m_IsReadOnly = (byte)(isReadOnly ? 1 : 0);
            _internalCapacity = internalCapacity;
            m_useMemoryInitPattern = (byte)(useMemoryInitPattern ? 1 : 0);
            m_memoryInitPattern = memoryInitPattern;
            ElementSize = elementSize;
            _alignOf = alignOf;
        }

#else
        internal UntypedDynamicBuffer(BufferHeader* header, int internalCapacity, int elementSize, int alignOf)
        {
            _buffer = header;
            _internalCapacity = internalCapacity;
            ElementSize = elementSize;
            _alignOf = alignOf;
        }
#endif

        public int Length
        {
            get
            {
                CheckReadAccess();
                return _buffer->Length;
            }
            set => ResizeUninitialized(value);
        }

        /// <summary>
        /// Cannot be smaller than Length. Every capacity change may reallocate; small increments do not reserve extra growth space.
        /// </summary>
        public int Capacity
        {
            get
            {
                CheckReadAccess();
                return _buffer->Capacity;
            }
            set
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
                if (value < Length)
                {
                    throw new InvalidOperationException($"Capacity {value} can't be set smaller than Length {Length}");
                }
#endif
                CheckWriteAccessAndInvalidateArrayAliases();
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                BufferHeader.SetCapacity(_buffer, value, ElementSize, _alignOf, BufferHeader.TrashMode.RetainOldData,
                    m_useMemoryInitPattern == 1, m_memoryInitPattern, _internalCapacity);
#else
                BufferHeader.SetCapacity(
                    _buffer, value, ElementSize, _alignOf, BufferHeader.TrashMode.RetainOldData, false, 0, _internalCapacity);
#endif
            }
        }

        public bool IsEmpty => !IsCreated || Length == 0;

        public bool IsCreated => _buffer != null;

        public int ElementSize { get; }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        private void CheckBounds(int index)
        {
            if ((uint)index >= (uint)Length)
            {
                throw new IndexOutOfRangeException($"Index {index} is out of range in DynamicBuffer of '{Length}' Length.");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void CheckReadAccess()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety0);
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety1);
#endif
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void CheckWriteAccess()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety0);
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety1);
#endif
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void CheckWriteAccessAndInvalidateArrayAliases()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety0);
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety1);
#endif
        }

        public void* this[int index]
        {
            get
            {
                CheckReadAccess();
                CheckBounds(index);
                return BufferHeader.GetElementPointer(_buffer) + (index * ElementSize);
            }

            set
            {
                CheckWriteAccess();
                CheckBounds(index);
                var dst = BufferHeader.GetElementPointer(_buffer) + (index * ElementSize);
                UnsafeUtility.MemCpy(dst, value, ElementSize);
            }
        }

        public void ResizeUninitialized(int length)
        {
            EnsureCapacity(length);
            _buffer->Length = length;
        }

        public void Resize(int length, NativeArrayOptions options)
        {
            EnsureCapacity(length);

            var oldLength = _buffer->Length;
            _buffer->Length = length;
            if (options == NativeArrayOptions.ClearMemory && oldLength < length)
            {
                var num = length - oldLength;
                var ptr = BufferHeader.GetElementPointer(_buffer);
                UnsafeUtility.MemClear(ptr + (oldLength * ElementSize), num * ElementSize);
            }
        }

        public void EnsureCapacity(int length)
        {
            CheckWriteAccessAndInvalidateArrayAliases();
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            BufferHeader.EnsureCapacity(_buffer, length, ElementSize, _alignOf, BufferHeader.TrashMode.RetainOldData,
                m_useMemoryInitPattern == 1, m_memoryInitPattern);
#else
            BufferHeader.EnsureCapacity(_buffer, length, ElementSize, _alignOf, BufferHeader.TrashMode.RetainOldData, false, 0);
#endif
        }

        /// <summary>
        /// Does not overwrite cleared memory or shrink capacity.
        /// </summary>
        public void Clear()
        {
            CheckWriteAccessAndInvalidateArrayAliases();

            _buffer->Length = 0;
        }

        public int Add(void* elem)
        {
            CheckWriteAccess();
            var length = Length;
            ResizeUninitialized(length + 1);
            this[length] = elem;
            return length;
        }

        public void AddRange(void* elem, int count)
        {
            CheckWriteAccess();
            var oldLength = Length;
            ResizeUninitialized(oldLength + count);

            void* basePtr = BufferHeader.GetElementPointer(_buffer) + (oldLength * ElementSize);
            UnsafeUtility.MemCpy(basePtr, elem, (long)ElementSize * count);
        }

        public void RemoveRange(int index, int count)
        {
            CheckWriteAccess();
            CheckBounds(index);
            if (count == 0)
            {
                return;
            }

            CheckBounds((index + count) - 1);

            var elemSize = ElementSize;
            var basePtr = BufferHeader.GetElementPointer(_buffer);

            UnsafeUtility.MemMove(basePtr + (index * elemSize), basePtr + ((index + count) * elemSize), (long)elemSize * (Length - count - index));

            _buffer->Length -= count;
        }

        public void RemoveAt(int index)
        {
            RemoveRange(index, 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void* GetUnsafePtr()
        {
            CheckWriteAccess();
            return BufferHeader.GetElementPointer(_buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void* GetUnsafeReadOnlyPtr()
        {
            CheckReadAccess();
            return BufferHeader.GetElementPointer(_buffer);
        }
    }
}
