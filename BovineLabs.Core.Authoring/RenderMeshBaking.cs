// <copyright file="RenderMeshBaking.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_GRAPHICS
namespace BovineLabs.Core.Authoring
{
    using Unity.Entities;
    using Unity.Mathematics;
    using Unity.Rendering;
    using UnityEngine;

    public static class RenderMeshBaking
    {
        public static void AddRendererComponents<T>(Baker<T> baker, Entity entity, in RenderMeshDescription renderMeshDescription, RenderMesh renderMesh)
            where T : Component
        {
            // Entities with Static are never rendered with motion vectors
            bool inMotionPass = RenderMeshUtility.kUseHybridMotionPass &&
                                renderMeshDescription.FilterSettings.IsInMotionPass &&
                                !baker.IsStatic();

            RenderMeshUtility.EntitiesGraphicsComponentFlags flags = RenderMeshUtility.EntitiesGraphicsComponentFlags.Baking;
            if (inMotionPass)
            {
                flags |= RenderMeshUtility.EntitiesGraphicsComponentFlags.InMotionPass;
            }

            flags |= RenderMeshUtility.LightProbeFlags(renderMeshDescription.LightProbeUsage);
            flags |= RenderMeshUtility.DepthSortedFlags(renderMesh.material);

            // Add all components up front using as few calls as possible.
            var componentTypes = RenderMeshUtility.s_EntitiesGraphicsComponentTypes.GetComponentTypes(flags);
            baker.AddComponent(entity, componentTypes);

            baker.SetSharedComponentManaged(entity, renderMesh);
            baker.SetSharedComponentManaged(entity, renderMeshDescription.FilterSettings);

            var localBounds = renderMesh.mesh.bounds.ToAABB();
            baker.SetComponent(entity, new RenderBounds { Value = localBounds });
        }
    }
}
#endif
