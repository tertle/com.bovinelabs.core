// <copyright file="FixedArray.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Collections
{
    using System.Runtime.CompilerServices;
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
                CollectionHelper.CheckIndexInRange(index, this.Length);
                return UnsafeUtility.ReadArrayElement<T>(this.Buffer, CollectionHelper.AssumePositive(index));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                CollectionHelper.CheckIndexInRange(index, this.Length);
                UnsafeUtility.WriteArrayElement(this.Buffer, CollectionHelper.AssumePositive(index), value);
            }
        }
    }
}
