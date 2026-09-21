namespace BovineLabs.Core.Collections
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    public unsafe partial struct NativeThreadStream
    {
        [NativeContainer]
        [NativeContainerIsAtomicWriteOnly]
        public readonly struct Writer
        {
            private readonly UnsafeThreadStream.Writer _writer;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Preserve Unity safety-handle field names.")]
            [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Preserve Unity safety-handle field names.")]
            internal readonly AtomicSafetyHandle m_Safety;
            [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Preserve the established static safety ID name.")]
            [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Preserve the established static safety ID name.")]
            internal static readonly SharedStatic<int> s_staticSafetyId = SharedStatic<int>.GetOrCreate<Writer>();
#endif

            internal Writer(ref NativeThreadStream stream)
            {
                _writer = stream._stream.AsWriter();

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                m_Safety = stream.m_Safety;
                CollectionHelper.SetStaticSafetyId(ref m_Safety, ref s_staticSafetyId.Data, "BovineLabs.Core.Collections.NativeThreadStream.Writer");
#endif
            }

            public void Write<T>(T value)
                where T : unmanaged
            {
                ref var dst = ref Allocate<T>();
                dst = value;
            }

            public ref T Allocate<T>()
                where T : unmanaged
            {
                var size = UnsafeUtility.SizeOf<T>();
                return ref UnsafeUtility.AsRef<T>(Allocate(size));
            }

            public byte* Allocate(int size)
            {
                CheckAllocateSize(size);
                return _writer.Allocate(size);
            }

            public void WriteLarge<T>(NativeArray<T> array)
                where T : unmanaged
            {
                var byteArray = array.Reinterpret<byte>(UnsafeUtility.SizeOf<T>());
                WriteLarge((byte*)byteArray.GetUnsafeReadOnlyPtr(), byteArray.Length);
            }

            public void WriteLarge<T>(NativeSlice<T> data)
                where T : unmanaged
            {
                var num = UnsafeUtility.SizeOf<T>();
                var countPerAllocate = MaxLargeSize / num;

                var allocationCount = data.Length / countPerAllocate;
                var allocationRemainder = data.Length % countPerAllocate;

                var maxSize = countPerAllocate * num;
                var maxOffset = data.Stride * countPerAllocate;

                var src = (byte*)data.GetUnsafeReadOnlyPtr();

                // Write the remainder first as this helps avoid an extra allocation most of the time
                // as you'd usually write at minimum the length beforehand
                if (allocationRemainder > 0)
                {
                    var dst = Allocate(allocationRemainder * num);
                    UnsafeUtility.MemCpyStride(dst, num, src + (allocationCount * maxOffset), data.Stride, num, allocationRemainder);
                }

                for (var i = 0; i < allocationCount; i++)
                {
                    var dst = Allocate(maxSize);
                    UnsafeUtility.MemCpyStride(dst, num, src + (i * maxOffset), data.Stride, num, countPerAllocate);
                }
            }

            public void WriteLarge(byte* data, int size)
            {
                var allocationCount = size / MaxLargeSize;
                var allocationRemainder = size % MaxLargeSize;

                // Write the remainder first as this helps avoid an extra allocation most of the time
                // as you'd usually write at minimum the length beforehand
                if (allocationRemainder > 0)
                {
                    var ptr = Allocate(allocationRemainder);
                    UnsafeUtility.MemCpy(ptr, data + (allocationCount * MaxLargeSize), allocationRemainder);
                }

                for (var i = 0; i < allocationCount; i++)
                {
                    var ptr = Allocate(MaxLargeSize);
                    UnsafeUtility.MemCpy(ptr, data + (i * MaxLargeSize), MaxLargeSize);
                }
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            private void CheckAllocateSize(int size)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);

                if (size > UnsafeThreadStreamBlockData.AllocationSize - sizeof(void*))
                {
                    throw new ArgumentException("Allocation size is too large");
                }
#endif
            }
        }

        [NativeContainer]
        [NativeContainerIsAtomicWriteOnly]
        public readonly struct Writer<T>
            where T : unmanaged
        {
            private readonly UnsafeThreadStream.Writer _writer;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Preserve Unity safety-handle field names.")]
            [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Preserve Unity safety-handle field names.")]
            private readonly AtomicSafetyHandle m_Safety;
#endif

            internal Writer(ref NativeThreadStream stream)
            {
                _writer = stream._stream.AsWriter();

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                m_Safety = stream.m_Safety;
#endif
            }

            public void Write(T value)
            {
                ref var dst = ref Allocate();
                dst = value;
            }

            public ref T Allocate()
            {
                var size = UnsafeUtility.SizeOf<T>();
                return ref UnsafeUtility.AsRef<T>(Allocate(size));
            }

            private byte* Allocate(int size)
            {
                CheckAllocateSize(size);
                return _writer.Allocate(size);
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            private void CheckAllocateSize(int size)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);

                if (size > UnsafeThreadStreamBlockData.AllocationSize - sizeof(void*))
                {
                    throw new ArgumentException("Allocation size is too large");
                }
#endif
            }
        }
    }
}
