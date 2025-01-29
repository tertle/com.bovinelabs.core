// <copyright file="EntityCommandBufferExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Extensions
{
    using BovineLabs.Core.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    public static unsafe class EntityCommandBufferExtensions
    {
        private const int Align64BIT = 8;

        public static UntypedDynamicBuffer AddUntypedBuffer(this EntityCommandBuffer ecb, Entity e, ComponentType componentType)
        {
            ecb.EnforceSingleThreadOwnership();
            ecb.AssertDidNotPlayback();

            // TODO try use the m_BufferSafety and m_ArrayInvalidationSafety but they are private
            return ecb.m_Data->CreateUntypedBufferCommand(ECBCommand.AddBuffer, &ecb.m_Data->m_MainThreadChain, ecb.MainThreadSortKey, e, componentType);
        }

        public static UntypedDynamicBuffer AddUntypedBuffer(this EntityCommandBuffer.ParallelWriter ecb, int sortKey, Entity e, ComponentType componentType)
        {
            return ecb.m_Data->CreateUntypedBufferCommand(ECBCommand.AddBuffer, &ecb.m_Data->m_MainThreadChain, sortKey, e, componentType);
        }

        public static void UnsafeAddComponent(this EntityCommandBuffer ecb, Entity e, TypeIndex typeIndex, int typeSize, void* componentDataPtr)
        {
            ecb.UnsafeAddComponent(e, typeIndex, typeSize, componentDataPtr);
        }

        public static void UnsafeAddComponent(
            this EntityCommandBuffer.ParallelWriter ecb, int sortIndex, Entity e, ComponentType componentType, void* componentDataPtr)
        {
            ref readonly var type = ref TypeManager.GetTypeInfo(componentType.TypeIndex);
            UnsafeAddComponent(ecb, sortIndex, e, componentType.TypeIndex, type.ElementSize, componentDataPtr);
        }

        public static void UnsafeAddComponent(
            this EntityCommandBuffer.ParallelWriter ecb, int sortIndex, Entity e, TypeIndex typeIndex, int typeSize, void* componentDataPtr)
        {
            ecb.UnsafeAddComponent(sortIndex, e, typeIndex, typeSize, componentDataPtr);
        }

        private static UntypedDynamicBuffer CreateUntypedBufferCommand(
            ref this EntityCommandBufferData ecbd, ECBCommand commandType, EntityCommandBufferChain* chain, int sortKey, Entity e, ComponentType componentType)
        {
            int internalCapacity;
            var header = ecbd.AddEntityBufferCommandUntyped(chain, sortKey, commandType, e, componentType, out internalCapacity);
            ref readonly var type = ref TypeManager.GetTypeInfo(componentType.TypeIndex);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var safety = AtomicSafetyHandle.GetTempMemoryHandle();
            AtomicSafetyHandle.UseSecondaryVersion(ref safety);
            var arraySafety = AtomicSafetyHandle.GetTempMemoryHandle();
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

            ecbd.ResetCommandBatching(chain);
            var cmd = (EntityBufferCommand*)ecbd.Reserve(chain, sortKey, sizeNeeded);

            cmd->Header.Header.CommandType = op;
            cmd->Header.Header.TotalSize = sizeNeeded;
            cmd->Header.Header.SortKey = chain->m_LastSortKey;
            cmd->Header.Entity = e;
            cmd->Header.IdentityIndex = 0;
            cmd->Header.BatchCount = 1;
            cmd->ComponentTypeIndex = typeIndex;
            cmd->ComponentSize = (short)type.SizeInChunk;
            cmd->ValueRequiresEntityFixup = 0;

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

            if (TypeManager.HasEntityReferences(typeIndex))
            {
                cmd->ValueRequiresEntityFixup = 1;
                ecbd.m_BufferWithFixups.Add(1);
            }

            return header;
        }
    }
}
