// <copyright file="NetUtility.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_NETCODE
namespace BovineLabs.Core.Utility
{
    using Unity.Entities;
    using Unity.NetCode;

    public static class NetUtility
    {
        public static Entity CreateRPC(EntityCommandBuffer commandBuffer, Entity connectionEntity, ComponentType componentType)
        {
            var entity = commandBuffer.CreateEntity();
            commandBuffer.AddComponent(entity, new SendRpcCommandRequest { TargetConnection = connectionEntity });
            commandBuffer.AddComponent(entity, componentType);
            return entity;
        }

        public static Entity CreateRPC<T>(EntityCommandBuffer commandBuffer, Entity connectionEntity)
            where T : unmanaged, IRpcCommand
        {
            return CreateRPC(commandBuffer, connectionEntity, ComponentType.ReadWrite<T>());
        }

        public static Entity CreateRPC<T>(EntityCommandBuffer commandBuffer, Entity connectionEntity, T componentData)
            where T : unmanaged, IRpcCommand
        {
            var entity = CreateRPC<T>(commandBuffer, connectionEntity);
            commandBuffer.SetComponent(entity, componentData);
            return entity;
        }

        public static Entity CreateApprovalRPC<T>(EntityCommandBuffer commandBuffer, Entity connectionEntity, T componentData)
            where T : unmanaged, IApprovalRpcCommand
        {
            var entity = CreateRPC(commandBuffer, connectionEntity, ComponentType.ReadWrite<T>());
            commandBuffer.SetComponent(entity, componentData);
            return entity;
        }
    }
}
#endif