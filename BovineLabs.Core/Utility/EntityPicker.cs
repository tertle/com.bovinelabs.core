// <copyright file="EntityPicker.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_GRAPHICS
namespace BovineLabs.Core.Utility
{
    using System;
    using System.Collections.Generic;
    using BovineLabs.Core.Extensions;
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
    using Unity.Jobs;
    using Unity.Mathematics;
    using Unity.Rendering;
    using Unity.Transforms;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.Rendering;
    using Object = UnityEngine.Object;

    public unsafe class EntityPicker : IDisposable
    {
        private readonly List<Matrix4x4> matrices = new();
        private readonly List<Vector4> colors = new();
        private readonly int colorPropertyID = Shader.PropertyToID("_SelectionColor");
        private readonly int colorSpaceID = Shader.PropertyToID("_ColorSpace");

        private readonly MaterialPropertyBlock propertyBlock;
        private readonly CommandBuffer commandBuffer;
        private readonly Material material;
        private readonly Material materialSkinned;
        private readonly SystemBase system;
        private Texture2D texture;
        private EntityQuery renderMeshQuery;
        private EntityQuery skinnedMeshQuery;

        public EntityPicker(SystemBase system)
        {
            this.system = system;

            this.propertyBlock = new MaterialPropertyBlock();
            this.commandBuffer = new CommandBuffer();
            this.material = new Material(Shader.Find("BovineLabs/Selection")) { enableInstancing = true };
            this.materialSkinned = new Material(Shader.Find("BovineLabs/SelectionSkinned"));
            this.texture = new Texture2D(1, 1);

            this.renderMeshQuery = system.GetEntityQuery(ComponentType.ReadOnly<RenderMesh>(), ComponentType.ReadOnly<LocalToWorld>());
            this.skinnedMeshQuery = system.GetEntityQuery(ComponentType.ReadOnly<SkinnedMeshRenderer>());
        }

        public void CompleteDependency()
        {
            this.renderMeshQuery.CompleteDependency();
            this.skinnedMeshQuery.CompleteDependency();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.commandBuffer.Dispose();

#if UNITY_EDITOR
            if (EditorApplication.isPlaying)
            {
                Object.Destroy(this.material);
                Object.Destroy(this.materialSkinned);
                Object.Destroy(this.texture);
            }
            else
#endif
            {
                Object.DestroyImmediate(this.material);
                Object.DestroyImmediate(this.materialSkinned);
                Object.DestroyImmediate(this.texture);
            }
        }

        public Entity Pick(float2 screenPosition, Camera camera, JobHandle jobHandle)
        {
            if (this.texture == null)
            {
                // This dies in editor for some reason sometimes
                this.texture = new Texture2D(1, 1);
            }

            var renderTexture = RenderTexture.GetTemporary(
            camera.pixelWidth, camera.pixelHeight, 24, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);

            renderTexture.antiAliasing = 1;
            renderTexture.filterMode = FilterMode.Point;
            renderTexture.autoGenerateMips = false;

            this.commandBuffer.SetRenderTarget(renderTexture);
            this.commandBuffer.ClearRenderTarget(true, true, new Color(0, 0, 0, 0));
            this.commandBuffer.SetViewProjectionMatrices(camera.worldToCameraMatrix, camera.projectionMatrix);

            var map = new NativeParallelHashMap<Color, Entity>(this.renderMeshQuery.CalculateEntityCount(), Allocator.TempJob);

            this.AddRenderMeshes(map, jobHandle);
            this.AddSkinnedMeshRenderers(map);

            Graphics.ExecuteCommandBuffer(this.commandBuffer);

            RenderTexture.active = renderTexture;
            this.texture.ReadPixels(new Rect(screenPosition.x, screenPosition.y, 1, 1), 0, 0);
            var color = this.texture.GetPixel(0, 0);
            map.TryGetValue(color, out var result);
            RenderTexture.active = null;

            map.Dispose();

            RenderTexture.ReleaseTemporary(renderTexture);

            return result;
        }

        private static Color IndexToColor(int index)
        {
            var r = (byte)(index & 0xFF);
            var g = (byte)((index >> 8) & 0xFF);
            var b = (byte)((index >> 16) & 0xFF);
            var a = (byte)(255 - (index >> 24)); // flip the A just to make it easier to read since it'll likely never be used anyway
            return new Color32(r, g, b, a);
        }

        private void AddRenderMeshes(NativeParallelHashMap<Color, Entity> map, JobHandle jobHandle)
        {
            this.renderMeshQuery.CompleteDependency();

            var chunks = new NativeList<Chunk>(this.renderMeshQuery.CalculateChunkCount(), Allocator.TempJob);

            new GatherRenderMeshJob
                {
                    EntityType = this.system.GetEntityTypeHandle(),
                    RenderMeshType = this.system.GetSharedComponentTypeHandle<RenderMesh>(),
                    LocalToWorldType = this.system.GetComponentTypeHandle<LocalToWorld>(true),
                    Output = chunks.AsParallelWriter(),
                    Map = map.AsParallelWriter(),
                }
                .ScheduleParallel(this.renderMeshQuery, jobHandle).Complete();

            foreach (var chunk in chunks)
            {
                this.matrices.Clear();
                this.colors.Clear();
                this.propertyBlock.Clear();

                this.colors.AddRangeNative(chunk.Colors.Ptr, chunk.Colors.Length);
                this.propertyBlock.SetVectorArray(this.colorPropertyID, this.colors);

                this.matrices.AddRangeNative(chunk.Transforms.Ptr, chunk.Transforms.Length);
                var matrixArray = NoAllocHelpers.ExtractArrayFromListT(this.matrices);

                var mesh = this.system.World.EntityManager.GetSharedComponentManaged<RenderMesh>(chunk.Mesh);

                this.commandBuffer.DrawMeshInstanced(mesh.mesh, mesh.subMesh, this.material, -1, matrixArray, this.matrices.Count, this.propertyBlock);
            }

            chunks.Dispose();
        }

        private void AddSkinnedMeshRenderers(NativeParallelHashMap<Color, Entity> map)
        {
            this.skinnedMeshQuery.CompleteDependency();

            var entities = this.skinnedMeshQuery.ToEntityArray(Allocator.Temp);

            foreach (var entity in entities)
            {
                var renderer = this.system.EntityManager.GetComponentObject<SkinnedMeshRenderer>(entity);

                var color = IndexToColor(entity.Index);
                map.Add(color, entity);

                this.propertyBlock.Clear();
                this.propertyBlock.SetColor(this.colorPropertyID, color);
                this.propertyBlock.SetInt(this.colorSpaceID, 1);
                renderer.SetPropertyBlock(this.propertyBlock);

                for (var i = 0; i < renderer.sharedMesh.subMeshCount; i++)
                {
                    this.commandBuffer.DrawRenderer(renderer, this.materialSkinned, i, 0);
                }
            }
        }

        [BurstCompile]
        private struct GatherRenderMeshJob : IJobChunk
        {
            [ReadOnly]
            public EntityTypeHandle EntityType;

            [ReadOnly]
            public SharedComponentTypeHandle<RenderMesh> RenderMeshType;

            [ReadOnly]
            public ComponentTypeHandle<LocalToWorld> LocalToWorldType;

            public NativeList<Chunk>.ParallelWriter Output;

            public NativeParallelHashMap<Color, Entity>.ParallelWriter Map;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var colors = new UnsafeList<Vector4>(chunk.Count, Allocator.Persistent);
                var transforms = new UnsafeList<Matrix4x4>(chunk.Count, Allocator.Persistent);

                var entities = chunk.GetNativeArray(this.EntityType);
                var localToWorlds = chunk.GetNativeArray(this.LocalToWorldType);
                var mesh = chunk.GetSharedComponentIndex(this.RenderMeshType);

                for (var index = 0; index < entities.Length; index++)
                {
                    Vector4 c = IndexToColor(entities[index].Index);
                    colors.AddNoResize(c);
                }

                transforms.AddRange(localToWorlds.GetUnsafeReadOnlyPtr(), localToWorlds.Length);
                this.Map.AddBatchUnsafe((Color*)colors.Ptr, (Entity*)entities.GetUnsafeReadOnlyPtr(), entities.Length);

                this.Output.AddNoResize(new Chunk
                {
                    Mesh = mesh,
                    Colors = colors,
                    Transforms = transforms,
                });
            }
        }

        private struct Chunk
        {
            public int Mesh;
            public UnsafeList<Vector4> Colors;
            public UnsafeList<Matrix4x4> Transforms;
        }
    }
}
#endif
