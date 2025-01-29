// <copyright file="AssetLoadingSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_SUBSCENE
namespace BovineLabs.Core.SubScenes
{
    using System.Collections.Generic;
    using BovineLabs.Core.Groups;
    using Unity.Entities;
    using UnityEngine;

    [UpdateInGroup(typeof(AfterSceneSystemGroup), OrderFirst = true)]
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.ThinClientSimulation | Worlds.Service)]
    public partial class AssetLoadingSystem : SystemBase
    {
        private readonly Dictionary<GameObject, GameObject> objects = new();

        /// <inheritdoc />
        protected override void OnCreate()
        {
            this.RequireForUpdate<AssetLoad>();
        }

        /// <inheritdoc />
        protected override void OnDestroy()
        {
            foreach (var o in this.objects)
            {
                Object.Destroy(o.Value);
            }
        }

        /// <inheritdoc />
        protected override void OnUpdate()
        {
            var flags = this.World.Flags;

            var ecb = SystemAPI.GetSingleton<InstantiateCommandBufferSystem.Singleton>().CreateCommandBuffer(this.World.Unmanaged);

            foreach (var (assetLoads, entity) in SystemAPI.Query<DynamicBuffer<AssetLoad>>().WithEntityAccess())
            {
                for (var index = assetLoads.Length - 1; index >= 0; index--)
                {
                    var asset = assetLoads[index];
                    if ((asset.TargetWorld & flags) == 0)
                    {
                        continue;
                    }

                    // This is for live baking
                    if (this.objects.TryGetValue(asset.Asset.Value, out var instance))
                    {
                        Object.Destroy(instance);
                    }

                    instance = Object.Instantiate(asset.Asset.Value);
                    Object.DontDestroyOnLoad(instance);
                    this.objects[asset.Asset.Value] = instance;
                }

                ecb.AddComponent<Disabled>(entity);
            }
        }
    }
}
#endif
