// <copyright file="PhysicsDebugDraw.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BL_DRAW && UNITY_PHYSICS
namespace BovineLabs.Core.Debug.PhysicsDrawers
{
    using Unity.Entities;

    public struct PhysicsDebugDraw : IComponentData
    {
        public bool DrawColliderEdges;
        public bool DrawMeshColliderEdges;
        public bool DrawColliderAabbs;
        public bool DrawCollisionEvents;
        public bool DrawTriggerEvents;
    }
}
#endif
