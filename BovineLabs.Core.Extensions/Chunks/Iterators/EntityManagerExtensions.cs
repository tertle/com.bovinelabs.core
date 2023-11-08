// <copyright file="EntityManagerExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BL_ENABLE_LINKED_CHUNKS
namespace BovineLabs.Core.Chunks.Iterators
{
    using Unity.Entities;

    public static unsafe class EntityManagerExtensions
    {
        public static VirtualEntityTypeHandle GetVirtualEntityTypeHandle(this EntityManager entityManager)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var access = entityManager.GetCheckedEntityDataAccess();
            return new VirtualEntityTypeHandle(
                access->DependencyManager->Safety.GetSafetyHandleForEntityTypeHandle());
#else
            return new VirtualEntityTypeHandle(false);
#endif
        }

        public static VirtualComponentTypeHandle<T> GetVirtualComponentTypeHandle<T>(this EntityManager entityManager, bool isReadOnly)
            where T : unmanaged, IComponentData
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var access = entityManager.GetCheckedEntityDataAccess();
            var typeIndex = TypeManager.GetTypeIndex<T>();
            return new VirtualComponentTypeHandle<T>(
                access->DependencyManager->Safety.GetSafetyHandleForComponentTypeHandle(typeIndex, isReadOnly),
                isReadOnly,
                entityManager.GlobalSystemVersion);
#else
            return new VirtualComponentTypeHandle<T>(isReadOnly, entityManager.GlobalSystemVersion);
#endif
        }

        public static VirtualBufferTypeHandle<T> GetVirtualBufferTypeHandle<T>(this EntityManager entityManager, bool isReadOnly)
            where T : unmanaged, IBufferElementData
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var access = entityManager.GetCheckedEntityDataAccess();
            var typeIndex = TypeManager.GetTypeIndex<T>();
            return new VirtualBufferTypeHandle<T>(
                access->DependencyManager->Safety.GetSafetyHandleForBufferTypeHandle(typeIndex, isReadOnly),
                access->DependencyManager->Safety.GetBufferHandleForBufferTypeHandle(typeIndex),
                isReadOnly,
                entityManager.GlobalSystemVersion);
#else
            return new VirtualBufferTypeHandle<T>(isReadOnly, entityManager.GlobalSystemVersion);
#endif
        }

        public static VirtualComponentLookup<T> GetVirtualComponentLookup<T>(this EntityManager entityManager, bool isReadOnly)
            where T : unmanaged, IComponentData
        {
            var access = entityManager.GetCheckedEntityDataAccess();
            var typeIndex = TypeManager.GetTypeIndex<T>();

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            return new VirtualComponentLookup<T>(typeIndex, access, isReadOnly);
#else
            return new VirtualComponentLookup<T>(typeIndex, access);
#endif
        }
    }
}
#endif
