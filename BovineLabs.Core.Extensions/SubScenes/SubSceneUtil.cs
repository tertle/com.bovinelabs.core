// <copyright file="SubSceneUtil.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.SubScenes
{
    using Unity.Entities;
    using Unity.Scenes;

    public static class SubSceneUtil
    {
        public static void UnloadScene(EntityCommandBuffer entityCommandBuffer, Entity entity, ref BufferLookup<ResolvedSectionEntity> resolvedSectionEntitys)
        {
            entityCommandBuffer.RemoveComponent<RequestSceneLoaded>(entity);

            if (resolvedSectionEntitys.TryGetBuffer(entity, out var resolvedSectionEntity))
            {
                foreach (var section in resolvedSectionEntity.AsNativeArray())
                {
                    entityCommandBuffer.RemoveComponent<RequestSceneLoaded>(section.SectionEntity);
                }
            }
        }

        public static void LoadScene(EntityCommandBuffer entityCommandBuffer, Entity entity, ref BufferLookup<ResolvedSectionEntity> resolvedSectionEntitys)
        {
            entityCommandBuffer.AddComponent<RequestSceneLoaded>(entity);

            if (resolvedSectionEntitys.TryGetBuffer(entity, out var resolvedSectionEntity))
            {
                foreach (var section in resolvedSectionEntity.AsNativeArray())
                {
                    entityCommandBuffer.AddComponent<RequestSceneLoaded>(section.SectionEntity);
                }
            }
        }
    }
}
