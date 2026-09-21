namespace BovineLabs.Core.Collections
{
    using System.Runtime.CompilerServices;
    using BovineLabs.Core.Internal;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    public unsafe struct FixedArray<T, TS>
        where T : unmanaged
        where TS : unmanaged
    {
        private TS _data;

        public readonly int Length => sizeof(TS) / sizeof(T);

        private readonly T* Buffer
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                fixed (void* ptr = &_data)
                {
                    return (T*)ptr;
                }
            }
        }

        public T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get
            {
                CollectionChecks.CheckIndexInRange(index, Length);
                return UnsafeUtility.ReadArrayElement<T>(Buffer, CollectionChecks.AssumePositive(index));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                CollectionChecks.CheckIndexInRange(index, Length);
                UnsafeUtility.WriteArrayElement(Buffer, CollectionChecks.AssumePositive(index), value);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T ElementAt(int index)
        {
            CollectionChecks.CheckIndexInRange(index, Length);
            return ref UnsafeUtility.ArrayElementAsRef<T>(Buffer, CollectionChecks.AssumePositive(index));
        }
    }
}
