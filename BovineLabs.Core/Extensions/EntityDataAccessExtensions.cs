// <copyright file="EntityDataAccessExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Extensions
{
    using System;
    using BovineLabs.Core.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    public static unsafe class EntityDataAccessExtensions
    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal static UntypedDynamicBuffer GetUntypedBuffer(
            ref this EntityDataAccess access, ComponentType componentType, Entity entity, AtomicSafetyHandle safety, AtomicSafetyHandle arrayInvalidationSafety,
            bool isReadOnly = false)
#else
        internal static UntypedDynamicBuffer GetUntypedBuffer(
            ref this EntityDataAccess access, ComponentType componentType, Entity entity, bool isReadOnly = false)
#endif
        {
            var typeIndex = componentType.TypeIndex;

#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
            access.EntityComponentStore->AssertEntityHasComponent(entity, typeIndex);
            if (!TypeManager.IsBuffer(typeIndex))
            {
                var typeName = typeIndex.ToFixedString();

                throw new ArgumentException(
                    $"GetBuffer<{typeName}> may not be IComponentData or ISharedComponentData; currently {TypeManager.GetTypeInfo(typeIndex).Category}");
            }
#endif

            if (!access.IsInExclusiveTransaction)
            {
                if (isReadOnly)
                {
                    access.DependencyManager->CompleteWriteDependency(typeIndex);
                }
                else
                {
                    access.DependencyManager->CompleteReadAndWriteDependency(typeIndex);
                }
            }

            BufferHeader* header;
            if (isReadOnly)
            {
                header = (BufferHeader*)access.EntityComponentStore->GetComponentDataWithTypeRO(entity, typeIndex);
            }
            else
            {
                header = (BufferHeader*)access.EntityComponentStore->GetComponentDataWithTypeRW(entity, typeIndex,
                    access.EntityComponentStore->GlobalSystemVersion);
            }

            var internalCapacity = TypeManager.GetTypeInfo(typeIndex).BufferCapacity;
            var typeInfo = TypeManager.GetTypeInfo(typeIndex);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var useMemoryInit = access.EntityComponentStore->useMemoryInitPattern != 0;
            var memoryInitPattern = access.EntityComponentStore->memoryInitPattern;

            return new UntypedDynamicBuffer(header, safety, arrayInvalidationSafety, isReadOnly, useMemoryInit, memoryInitPattern, internalCapacity,
                typeInfo.ElementSize, UntypedDynamicBuffer.AlignOf);
#else
            return new UntypedDynamicBuffer(header, internalCapacity, typeInfo.ElementSize, UntypedDynamicBuffer.AlignOf);
#endif
        }
    }
}
