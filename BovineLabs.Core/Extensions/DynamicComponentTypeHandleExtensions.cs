// <copyright file="DynamicComponentTypeHandleExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Extensions
{
    using Unity.Entities;

    public static class DynamicComponentTypeHandleExtensions
    {
        public static DynamicComponentTypeHandle Clone(this DynamicComponentTypeHandle handle, ComponentType componentType)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            return new DynamicComponentTypeHandle(componentType, handle.m_Safety0, handle.m_Safety1, handle.GlobalSystemVersion);
#else
            return new DynamicComponentTypeHandle(componentType, handle.GlobalSystemVersion);
#endif
        }
    }
}
