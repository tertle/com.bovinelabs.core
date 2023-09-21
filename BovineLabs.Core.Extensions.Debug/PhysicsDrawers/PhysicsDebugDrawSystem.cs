// <copyright file="PhysicsDebugDrawSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BL_DRAW && UNITY_PHYSICS
namespace BovineLabs.Core.Debug.PhysicsDrawers
{
    using BovineLabs.Core;
    using BovineLabs.Core.Internal;
    using BovineLabs.Draw;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Jobs;
    using Unity.Mathematics;
    using Unity.Physics;
    using Unity.Transforms;
    using UnityEngine;
    // using Unity.Physics;
    using BoxCollider = Unity.Physics.BoxCollider;
    using CapsuleCollider = Unity.Physics.CapsuleCollider;
    using Collider = Unity.Physics.Collider;
    using Mesh = Unity.Physics.Mesh;
    using MeshCollider = Unity.Physics.MeshCollider;
    using SphereCollider = Unity.Physics.SphereCollider;
    using TerrainCollider = Unity.Physics.TerrainCollider;

    [UpdateInGroup(typeof(DebugSystemGroup))]
    public partial struct PhysicsDebugDrawSystem : ISystem
    {
        /// <inheritdoc />
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton(out PhysicsDebugDraw debug))
            {
                return;
            }

            if (debug is { DrawColliderEdges: false, DrawColliderAabbs: false })
            {
                return;
            }

            var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;
            var bodies = physicsWorld.Bodies;

            var drawer = SystemAPI.GetSingleton<DrawSystem.Singleton>().CreateDrawer();

            if (debug.DrawColliderEdges)
            {
                state.Dependency = new DrawCollidersJob
                    {
                        Bodies = bodies,
                        NumDynamicBodies = physicsWorld.NumDynamicBodies,
                        Drawer = drawer,
                        DrawMeshColliderEdges = true || debug.DrawMeshColliderEdges,
                    }
                    .ScheduleParallel(bodies.Length, 1, state.Dependency);
            }

            if (debug.DrawColliderAabbs)
            {
                state.Dependency = new DrawAabbsJob
                    {
                        Bodies = bodies,
                        Drawer = drawer,
                    }
                    .ScheduleParallel(bodies.Length, 1, state.Dependency);
            }
        }

        [BurstCompile]
        private unsafe struct DrawCollidersJob : IJobFor
        {
            [ReadOnly]
            public NativeArray<RigidBody> Bodies;

            public Drawer Drawer;
            public int NumDynamicBodies;
            public bool DrawMeshColliderEdges;

            public void Execute(int index)
            {
                var body = this.Bodies[index];
                if (!body.Collider.IsCreated)
                {
                    return;
                }

                var color = index < this.NumDynamicBodies ? Color.red : Color.green;

                var transform = float4x4.TRS(body.WorldFromBody.pos, body.WorldFromBody.rot, body.Scale);

                this.DrawCollider((Collider*)body.Collider.GetUnsafePtr(), transform,color);
            }

            private void DrawCollider(Collider* collider, float4x4 transform, Color color)
            {
                switch (collider->Type)
                {
                    case ColliderType.Box:
                        this.DrawBox((BoxCollider*)collider, transform, color);
                        break;
                    case ColliderType.Triangle:
                        this.DrawTris((PolygonCollider*)collider, transform, color);
                        break;
                    case ColliderType.Quad:
                        this.DrawQuad((PolygonCollider*)collider, transform, color);
                        break;
                    case ColliderType.Cylinder:
                        this.DrawCylinder((CylinderCollider*)collider, transform, color);
                        break;
                    case ColliderType.Convex:
                        this.DrawConvex((ConvexCollider*)collider, transform, color);
                        break;
                    case ColliderType.Sphere:
                        this.DrawSphere((SphereCollider*)collider, transform, color);
                        break;
                    case ColliderType.Capsule:
                        this.DrawCapsule((CapsuleCollider*)collider, transform, color);
                        break;
                    case ColliderType.Compound:
                        this.DrawCompound((CompoundCollider*)collider, transform, color);
                        break;
                    case ColliderType.Mesh:
                        if (this.DrawMeshColliderEdges)
                        {
                            this.DrawMesh((MeshCollider*)collider, transform, color);
                        }

                        break;
                    case ColliderType.Terrain:
                        if (this.DrawMeshColliderEdges)
                        {
                            this.DrawTerrain((TerrainCollider*)collider, transform, color);
                        }

                        break;
                }
            }

            private void DrawBox(BoxCollider* collider, float4x4 transform, Color color)
            {
                var position = math.transform(transform, collider->Center);
                var orientation = math.mul(transform.Rotation(), collider->Orientation);
                var size = collider->Size * transform.Scale();
                this.Drawer.Cuboid(position, orientation, size, color);
            }

            private void DrawTris(PolygonCollider* collider, float4x4 transform, Color color)
            {
                this.Drawer.Triangle(
                    math.transform(transform, collider->Vertices[0]),
                    math.transform(transform, collider->Vertices[1]),
                    math.transform(transform, collider->Vertices[2]),
                    color);
            }

            private void DrawQuad(PolygonCollider* collider, float4x4 transform, Color color)
            {
                this.Drawer.Quad(
                    math.transform(transform, collider->Vertices[0]),
                    math.transform(transform, collider->Vertices[1]),
                    math.transform(transform, collider->Vertices[2]),
                    math.transform(transform, collider->Vertices[3]),
                    color);
            }

            private void DrawCylinder(CylinderCollider* collider, float4x4 transform, Color color)
            {
                var position = math.transform(transform, collider->Center);
                var orientation = math.mul(transform.Rotation(), collider->Orientation);
                var scale = math.cmax(transform.Scale());
                var height = scale * collider->Height;
                var radius = scale * collider->Radius;

                this.Drawer.Cylinder(position, orientation, height, radius, collider->SideCount, color);
            }

            private void DrawConvex(ConvexCollider* collider, float4x4 transform, Color color)
            {
                var faces = collider->GetNumFaces();
                for (var faceIndex = 0; faceIndex < faces; faceIndex++)
                {
                    var edges = collider->GetNumVerticesFromFace(faceIndex);

                    for (var edgeIndex = 0; edgeIndex < edges; edgeIndex++)
                    {
                        collider->GetEdgeFromFace(faceIndex, edgeIndex, out var from, out var to);

                        var p0 = math.transform(transform, from);
                        var p1 = math.transform(transform, to);
                        this.Drawer.Line(p0, p1, color);
                    }
                }
            }

            private void DrawSphere(SphereCollider* collider, float4x4 transform, Color color)
            {
                const int defaultColliderSideCount = 16;
                var center = math.transform(transform, collider->Center);
                var radius = collider->Radius * math.cmax(transform.Scale());

                this.Drawer.Sphere(center, radius, defaultColliderSideCount, color);
            }

            private void DrawCapsule(CapsuleCollider* collider, float4x4 transform, Color color)
            {
                var height = math.distance(collider->Vertex0, collider->Vertex1) + (2 * collider->Radius);
                var center = (collider->Vertex1 + collider->Vertex0) / 2f;

                var position = math.transform(transform, center);
                var rotation = transform.Rotation();
                var radius = math.cmax(transform.Scale()) * collider->Radius;

                this.Drawer.Capsule(position, rotation, height, radius, 20, color);
            }

            private void DrawCompound(CompoundCollider* collider, float4x4 transform, Color color)
            {
                for (var i = 0; i < collider->NumChildren; i++)
                {
                    ref var child = ref collider->Children[i];
                    var childCollider = child.Collider;
                    var worldFromChild = math.mul(transform, float4x4.TRS(child.CompoundFromChild.pos, child.CompoundFromChild.rot, new float3(1)));
                    this.DrawCollider(childCollider, worldFromChild, color);
                }
            }

            private void DrawMesh(MeshCollider* collider, float4x4 transform, Color color)
            {
                ref var mesh = ref collider->Mesh;

                for (var sectionIndex = 0; sectionIndex < mesh.Sections.Length; sectionIndex++)
                {
                    ref var section = ref mesh.Sections[sectionIndex];
                    for (var primitiveIndex = 0; primitiveIndex < section.PrimitiveVertexIndices.Length; primitiveIndex++)
                    {
                        var vertexIndices = section.PrimitiveVertexIndices[primitiveIndex];
                        var flags = section.PrimitiveFlags[primitiveIndex];
                        var numTriangles = (flags & Mesh.PrimitiveFlags.IsTrianglePair) != 0 ? 2 : 1;

                        var v = new float3x4(
                            section.Vertices[vertexIndices.A],
                            section.Vertices[vertexIndices.B],
                            section.Vertices[vertexIndices.C],
                            section.Vertices[vertexIndices.D]);

                        for (var triangleIndex = 0; triangleIndex < numTriangles; triangleIndex++)
                        {
                            var a = math.transform(transform, v[0]);
                            var b = math.transform(transform, v[1 + triangleIndex]);
                            var c = math.transform(transform, v[2 + triangleIndex]);

                            this.Drawer.Triangle(a, b, c, color);
                        }
                    }
                }
            }

            private void DrawTerrain(TerrainCollider* collider, float4x4 transform, Color color)
            {
                ref var terrain = ref collider->Terrain;

                for (var i = 0; i < terrain.Size.x - 1; i++)
                {
                    for (var j = 0; j < terrain.Size.y - 1; j++)
                    {
                        int i0 = i;
                        int i1 = i + 1;
                        int j0 = j;
                        int j1 = j + 1;
                        var v0 = math.transform(transform, new float3(i0, terrain.Heights[i0 + (terrain.Size.x * j0)], j0) * terrain.Scale);
                        var v1 = math.transform(transform, new float3(i1, terrain.Heights[i1 + (terrain.Size.x * j0)], j0) * terrain.Scale);
                        var v2 = math.transform(transform, new float3(i0, terrain.Heights[i0 + (terrain.Size.x * j1)], j1) * terrain.Scale);
                        var v3 = math.transform(transform, new float3(i1, terrain.Heights[i1 + (terrain.Size.x * j1)], j1) * terrain.Scale);

                        this.Drawer.Triangle(v0, v1, v2, color);
                        this.Drawer.Triangle(v1, v2, v3, color);

                    }
                }
            }
        }

        [BurstCompile]
        private struct DrawAabbsJob : IJobFor
        {
            [ReadOnly]
            public NativeArray<RigidBody> Bodies;

            public Drawer Drawer;

            public void Execute(int index)
            {
                var body = this.Bodies[index];
                if (!body.Collider.IsCreated)
                {
                    return;
                }

                var aabb = this.Bodies[index].Collider.Value.CalculateAabb(this.Bodies[index].WorldFromBody, this.Bodies[index].Scale);
                this.Drawer.Cuboid(aabb.Center, quaternion.identity, aabb.Extents, Color.red);
            }
        }
    }
}
#endif
