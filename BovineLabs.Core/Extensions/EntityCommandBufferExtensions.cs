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

        public static UntypedDynamicBuffer AddUntypedBuffer(ref this EntityCommandBuffer ecb, Entity e, ComponentType componentType)
        {
            ecb.EnforceSingleThreadOwnership();
            ecb.AssertDidNotPlayback();
            return ecb.m_Data->CreateUntypedBufferCommand(ECBCommand.AddBuffer, &ecb.m_Data->m_MainThreadChain, ecb.MainThreadSortKey, e, componentType);
        }

        public static void UnsafeAddComponent(ref this EntityCommandBuffer ecb, Entity e, TypeIndex typeIndex, int typeSize, void* componentDataPtr)
        {
            ecb.UnsafeAddComponent(e, typeIndex, typeSize, componentDataPtr);
        }

        private static UntypedDynamicBuffer CreateUntypedBufferCommand(
            ref this EntityCommandBufferData ecbd, ECBCommand commandType, EntityCommandBufferChain* chain, int sortKey, Entity e, ComponentType componentType)
        {
            int internalCapacity;
            BufferHeader* header = ecbd.AddEntityBufferCommandUntyped(chain, sortKey, commandType, e, componentType, out internalCapacity);
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
            ref this EntityCommandBufferData ecbd, EntityCommandBufferChain* chain, int sortKey, ECBCommand op, Entity e, ComponentType componentType, out int internalCapacity)
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
            cmd->ComponentSize = type.SizeInChunk;

            BufferHeader* header = &cmd->BufferNode.TempBuffer;
            BufferHeader.Initialize(header, type.BufferCapacity);

            cmd->BufferNode.Prev = chain->m_Cleanup->BufferCleanupList;
            chain->m_Cleanup->BufferCleanupList = &(cmd->BufferNode);

            internalCapacity = type.BufferCapacity;

            if (TypeManager.HasEntityReferences(typeIndex))
            {
                if (op == ECBCommand.AddBuffer)
                {
                    ecbd.m_BufferWithFixups.Add(1);
                    cmd->Header.Header.CommandType = ECBCommand.AddBufferWithEntityFixUp;
                }
                else if (op == ECBCommand.SetBuffer)
                {
                    ecbd.m_BufferWithFixups.Add(1);
                    cmd->Header.Header.CommandType = ECBCommand.SetBufferWithEntityFixUp;
                }
            }

            return header;
        }
    }
}
