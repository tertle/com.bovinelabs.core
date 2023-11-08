// <copyright file="SystemStateExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BL_ENABLE_LINKED_CHUNKS
namespace BovineLabs.Core.Chunks.Iterators
{
    using Unity.Entities;

    public static class SystemStateExtensions
    {
        public static VirtualEntityTypeHandle GetVirtualEntityTypeHandle(ref this SystemState system)
        {
            return system.EntityManager.GetVirtualEntityTypeHandle();
        }

        public static VirtualComponentTypeHandle<T> GetVirtualComponentTypeHandle<T>(ref this SystemState system, bool isReadOnly = false)
            where T : unmanaged, IComponentData
        {
            system.AddReaderWriter(isReadOnly ? ComponentType.ReadOnly<T>() : ComponentType.ReadWrite<T>());
            return system.EntityManager.GetVirtualComponentTypeHandle<T>(isReadOnly);
        }

        public static VirtualBufferTypeHandle<T> GetVirtualBufferTypeHandle<T>(ref this SystemState system, bool isReadOnly = false)
            where T : unmanaged, IBufferElementData
        {
            system.AddReaderWriter(isReadOnly ? ComponentType.ReadOnly<T>() : ComponentType.ReadWrite<T>());
            return system.EntityManager.GetVirtualBufferTypeHandle<T>(isReadOnly);
        }

        public static VirtualComponentLookup<T> GetVirtualComponentLookup<T>(ref this SystemState system, bool isReadOnly = false)
            where T : unmanaged, IComponentData
        {
            system.AddReaderWriter(isReadOnly ? ComponentType.ReadOnly<T>() : ComponentType.ReadWrite<T>());
            return system.EntityManager.GetVirtualComponentLookup<T>(isReadOnly);
        }
    }
}
#endif
