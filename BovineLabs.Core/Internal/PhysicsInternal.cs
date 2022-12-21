// <copyright file="PhysicsInternal.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_PHYSICS
namespace BovineLabs.Core.Internal
{
    using Unity.Mathematics;
    using Unity.Physics;

    public static class PhysicsInternal
    {
        public static int GetNumFaces(this ref ConvexCollider collider)
        {
            return collider.ConvexHull.Faces.Length;
        }

        public static int GetNumVerticesFromFace(this ref ConvexCollider collider, int faceIndex)
        {
            return collider.ConvexHull.Faces[faceIndex].NumVertices;
        }

        public static void GetEdgeFromFace(this ref ConvexCollider collider, int faceIndex, int edgeIndex, out float3 from, out float3 to)
        {
            var fromIndex = collider.ConvexHull.FaceVertexIndices[collider.ConvexHull.Faces[faceIndex].FirstIndex + edgeIndex];
            var toIndex = collider.ConvexHull.FaceVertexIndices[
                collider.ConvexHull.Faces[faceIndex].FirstIndex + ((edgeIndex + 1) % collider.ConvexHull.Faces[faceIndex].NumVertices)];

            from = collider.ConvexHull.Vertices[fromIndex];
            to = collider.ConvexHull.Vertices[toIndex];
        }

        public static bool GetRespondsToCollision(this ref Collider collider)
        {
            return collider.RespondsToCollision;
        }
    }
}
#endif
