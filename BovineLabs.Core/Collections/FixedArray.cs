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
        private TS data;

        public readonly int Length => sizeof(TS) / sizeof(T);

        private readonly T* Buffer
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                fixed (void* ptr = &this.data)
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
                CollectionChecks.CheckIndexInRange(index, this.Length);
                return UnsafeUtility.ReadArrayElement<T>(this.Buffer, CollectionChecks.AssumePositive(index));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                CollectionChecks.CheckIndexInRange(index, this.Length);
                UnsafeUtility.WriteArrayElement(this.Buffer, CollectionChecks.AssumePositive(index), value);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T ElementAt(int index)
        {
            CollectionChecks.CheckIndexInRange(index, this.Length);
            return ref UnsafeUtility.ArrayElementAsRef<T>(this.Buffer, CollectionChecks.AssumePositive(index));
        }
    }
}
