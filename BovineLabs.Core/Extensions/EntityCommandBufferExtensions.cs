// <copyright file="EntityCommandBufferExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Extensions
{
    using System;
    using BovineLabs.Core.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    public static unsafe class EntityCommandBufferExtensions
    {
        private const int Align64BIT = 8;

        public static UntypedDynamicBuffer AddUntypedBuffer(this EntityCommandBuffer.ParallelWriter ecb, int sortKey, Entity e, ComponentType componentType)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
            if (ecb.m_Data == null)
            {
                throw new NullReferenceException("The EntityCommandBuffer has not been initialized.");
            }
#endif
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(ecb.m_Safety0);
#endif
            var chain = ecb.m_ThreadIndex >= 0 ? &ecb.m_Data->m_ThreadedChains[ecb.m_ThreadIndex] : &ecb.m_Data->m_MainThreadChain;
            return ecb.CreateUntypedBufferCommand(ECBCommand.AddBuffer, chain, sortKey, e, componentType);
        }

        public static void UnsafeAddComponent(
            this EntityCommandBuffer.ParallelWriter ecb, int sortIndex, Entity e, TypeIndex typeIndex, int typeSize, void* componentDataPtr)
        {
            ecb.UnsafeAddComponent(sortIndex, e, typeIndex, typeSize, componentDataPtr);
        }

        private static UntypedDynamicBuffer CreateUntypedBufferCommand(
            ref this EntityCommandBuffer.ParallelWriter ecb, ECBCommand commandType, EntityCommandBufferChain* chain, int sortKey, Entity e,
            ComponentType componentType)
        {
            int internalCapacity;
            var header = ecb.m_Data->AddEntityBufferCommandUntyped(chain, sortKey, commandType, e, componentType, out internalCapacity);
            ref readonly var type = ref TypeManager.GetTypeInfo(componentType.TypeIndex);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var safety = ecb.m_BufferSafety;
            AtomicSafetyHandle.UseSecondaryVersion(ref safety);
            var arraySafety = ecb.m_ArrayInvalidationSafety;
            return new UntypedDynamicBuffer(header, safety, arraySafety, false, false, 0, internalCapacity, type.ElementSize, UntypedDynamicBuffer.AlignOf);
#else
            return new UntypedDynamicBuffer(header, internalCapacity, type.ElementSize, UntypedDynamicBuffer.AlignOf);
#endif
        }

        private static BufferHeader* AddEntityBufferCommandUntyped(
            ref this EntityCommandBufferData ecbd, EntityCommandBufferChain* chain, int sortKey, ECBCommand op, Entity e, ComponentType componentType,
            out int internalCapacity)
        {
            var typeIndex = componentType.TypeIndex;
            ref readonly var type = ref TypeManager.GetTypeInfo(typeIndex);
            var sizeNeeded = EntityCommandBufferData.Align(sizeof(EntityBufferCommand) + type.SizeInChunk, Align64BIT);

            var cmd = (EntityBufferCommand*)ecbd.Reserve(chain, sortKey, sizeNeeded);

            cmd->Header.Header.CommandType = op;
            cmd->Header.Header.TotalSize = sizeNeeded;
            cmd->Header.Header.SortKey = chain->m_LastSortKey;
            cmd->Header.Entity = e;
            cmd->Header.EntityCount = 0;
            cmd->Header.Entities = null;
            cmd->ComponentTypeIndex = typeIndex;
            cmd->ComponentSize = (short)type.SizeInChunk;

            var header = &cmd->BufferNode.TempBuffer;
            BufferHeader.Initialize(header, type.BufferCapacity);

            // Track all DynamicBuffer headers created during recording. Until the ECB is played back, it owns the
            // memory allocations for these buffers and is responsible for deallocating them when the ECB is disposed.
            cmd->BufferNode.Prev = chain->m_Cleanup->BufferCleanupList;
            chain->m_Cleanup->BufferCleanupList = &cmd->BufferNode;
            // The caller may invoke methods on the DynamicBuffer returned by this command during ECB recording which
            // cause it to allocate memory (for example, DynamicBuffer.AddRange). These allocations always use
            // Allocator.Persistent, not the ECB's allocator. These allocations must ALWAYS be manually cleaned up
            // if the ECB is disposed without being played back. So, we have to force the full ECB cleanup process
            // to run in this case, even if it could normally be skipped.
            ecbd.m_ForceFullDispose = true;

            internalCapacity = type.BufferCapacity;

            return header;
        }
    }
}
