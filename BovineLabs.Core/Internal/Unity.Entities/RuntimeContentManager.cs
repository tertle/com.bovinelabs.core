// <copyright file="RuntimeContentManager.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace Unity.Entities.Content
{
    using Unity.Collections;
    using Unity.Entities.Serialization;

    public static partial class RuntimeContentManager
    {
        internal static NativeArray<ContentArchiveId> GetArchiveIdsForValidation(AllocatorManager.AllocatorHandle allocator)
        {
            return Catalog.IsCreated
                ? Catalog.GetArchiveIds(allocator)
                : CollectionHelper.CreateNativeArray<ContentArchiveId>(0, allocator);
        }

        internal static NativeArray<ContentFileId> GetFileIdsForValidation(AllocatorManager.AllocatorHandle allocator)
        {
            return Catalog.IsCreated
                ? Catalog.GetFileIds(allocator)
                : CollectionHelper.CreateNativeArray<ContentFileId>(0, allocator);
        }

        internal static NativeArray<UntypedWeakReferenceId> GetObjectIdsForValidation(AllocatorManager.AllocatorHandle allocator)
        {
            return Catalog.IsCreated
                ? Catalog.GetObjectIds(allocator)
                : CollectionHelper.CreateNativeArray<UntypedWeakReferenceId>(0, allocator);
        }

        internal static NativeArray<UntypedWeakReferenceId> GetSceneIdsForValidation(AllocatorManager.AllocatorHandle allocator)
        {
            return Catalog.IsCreated
                ? Catalog.GetSceneIds(allocator)
                : CollectionHelper.CreateNativeArray<UntypedWeakReferenceId>(0, allocator);
        }
    }
}
