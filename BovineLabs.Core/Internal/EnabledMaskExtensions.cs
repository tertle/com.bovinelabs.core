// <copyright file="EnabledMaskExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Internal
{
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    public static unsafe class EnabledMaskExtensions
    {
        public static ulong* GetPtr(this EnabledMask mask)
        {
            ref var access = ref UnsafeUtility.As<EnabledMask, EnabledMaskClone>(ref mask);

            return UnsafeUtility.As<SafeBitRef, SafeBitRefClone>(ref access.m_EnableBitRef).m_Ptr;
        }

        public static int* GetDisableCount(this EnabledMask mask)
        {
            return UnsafeUtility.As<EnabledMask, EnabledMaskClone>(ref mask).m_PtrChunkDisabledCount;
        }

        public struct EnabledMaskClone
        {
            public SafeBitRef m_EnableBitRef;

            // pointer to chunk disabled count
            public int* m_PtrChunkDisabledCount;
        }

        public readonly struct SafeBitRefClone
        {
            public readonly ulong* m_Ptr;
            public readonly int m_OffsetInBits;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            public readonly AtomicSafetyHandle m_Safety;
#endif
        }
    }
}
