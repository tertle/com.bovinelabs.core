// <copyright file="BufferTypeHandleProxy.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Internal
{
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    public readonly struct BufferTypeHandleProxy<T>
        where T : unmanaged, IBufferElementData
    {
        internal readonly bool m_IsReadOnly;
        internal readonly uint m_GlobalSystemVersion;

        public BufferTypeHandleProxy(BufferTypeHandle<T> typeHandle)
        {
            this.m_GlobalSystemVersion = typeHandle.m_GlobalSystemVersion;
            this.m_IsReadOnly = typeHandle.IsReadOnly;
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        public BufferTypeHandle<T> ToBufferTypeHandle(AtomicSafetyHandle safety, AtomicSafetyHandle arrayInvalidationSafety)
        {
            return new BufferTypeHandle<T>(safety, arrayInvalidationSafety, this.m_IsReadOnly, this.m_GlobalSystemVersion);
        }
#else
        public BufferTypeHandle<T> ToBufferTypeHandle()
        {
            return new BufferTypeHandle<T>(this.m_IsReadOnly, this.m_GlobalSystemVersion);
        }
#endif
    }

    public static class BufferTypeHandleInternals
    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        public static AtomicSafetyHandle GetSafety0<T>(this BufferTypeHandle<T> componentTypeHandle)
            where T : unmanaged, IBufferElementData
        {
            return componentTypeHandle.m_Safety0;
        }

        public static AtomicSafetyHandle GetSafety1<T>(this BufferTypeHandle<T> componentTypeHandle)
            where T : unmanaged, IBufferElementData
        {
            return componentTypeHandle.m_Safety1;
        }
#endif
    }
}
