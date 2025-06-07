// <copyright file="SubScenePostLoadCommandBufferSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_SUBSCENE
namespace BovineLabs.Core.SubScenes
{
    using System;
    using System.Collections.Generic;
    using BovineLabs.Core.Groups;
    using BovineLabs.Core.Utility;
    using Unity.Collections;
    using Unity.Entities;
    using UnityEngine;
    using Hash128 = Unity.Entities.Hash128;

    public interface ICreatePostLoadCommandBuffer
    {
        public void Setup(World world, EntityCommandBuffer buffer, Hash128 sceneGuid);
    }

    [UpdateInGroup(typeof(BeforeSceneSystemGroup))]
    [WorldSystemFilter(Worlds.All)]
    public partial class SubScenePostLoadCommandBufferSystem : SystemBase
    {
        private readonly List<ICreatePostLoadCommandBuffer> creators = new();
        private EntityQuery query;

        protected override void OnCreate()
        {
            this.query = SystemAPI.QueryBuilder().WithAll<SceneReference>().WithNone<PostLoadCommandBuffer>().Build();
            this.RequireForUpdate(this.query);

            foreach (var p in ReflectionUtility.GetAllImplementations<ICreatePostLoadCommandBuffer>())
            {
                this.creators.Add((ICreatePostLoadCommandBuffer)Activator.CreateInstance(p));
            }

            if (this.creators.Count == 0)
            {
                this.Enabled = false;
            }

            List<Material> a = new List<Material>();
        }

        protected override void OnUpdate()
        {
            var sceneReferences = this.query.ToComponentDataArray<SceneReference>(this.WorldUpdateAllocator);
            var entities = this.query.ToEntityArray(this.WorldUpdateAllocator);

            for (var index = 0; index < entities.Length; index++)
            {
                var sceneEntity = entities[index];
                var sceneGuid = sceneReferences[index].SceneGUID;

                var ecb = new EntityCommandBuffer(Allocator.Persistent, PlaybackPolicy.MultiPlayback);

                foreach (var c in this.creators)
                {
                    c.Setup(this.World, ecb, sceneGuid);
                }

                this.EntityManager.AddComponentData(sceneEntity, new PostLoadCommandBuffer { CommandBuffer = ecb });
            }
        }
    }
}
#endif