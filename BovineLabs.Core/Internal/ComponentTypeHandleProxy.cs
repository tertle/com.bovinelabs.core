// <copyright file="ComponentTypeHandleProxy.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Internal
{
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    public readonly struct ComponentTypeHandleProxy<T>
    {
        public readonly uint m_GlobalSystemVersion;
        public readonly bool m_IsReadOnly;

        public ComponentTypeHandleProxy(ComponentTypeHandle<T> typeHandle)
        {
            this.m_GlobalSystemVersion = typeHandle.m_GlobalSystemVersion;
            this.m_IsReadOnly = typeHandle.IsReadOnly;
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        public ComponentTypeHandle<T> ToComponentTypeHandle(AtomicSafetyHandle safety)
        {
            return new ComponentTypeHandle<T>(safety, this.m_IsReadOnly, this.m_GlobalSystemVersion);
        }
#else
        public ComponentTypeHandle<T> ToComponentTypeHandle()
        {
            return new ComponentTypeHandle<T>(this.m_IsReadOnly, this.m_GlobalSystemVersion);
        }
#endif
    }

    public static class ComponentTypeHandleInternals
    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        public static AtomicSafetyHandle GetSafety<T>(this ComponentTypeHandle<T> componentTypeHandle)
        {
            return componentTypeHandle.m_Safety;
        }
#endif
    }
}
