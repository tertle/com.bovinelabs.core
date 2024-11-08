// <copyright file="BakingSettingsSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Authoring
{
    using System;
    using Unity.Entities;

    [Flags]
    public enum BakingFlags : uint
    {
        AddEntityGUID = 1 << 0,
        ForceStaticOptimization = 1 << 1,
        AssignName = 1 << 2,
        SceneViewLiveConversion = 1 << 3,
        GameViewLiveConversion = 1 << 4,
        IsBuildingForPlayer = 1 << 5,
    }

    [BakingType]
    public struct UnityBakingSettings : IComponentData
    {
        public Hash128 SceneGUID;
        public BakingFlags BakingFlags;
        public WorldSystemFilterFlags FilterFlags;
    }

    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    [UpdateInGroup(typeof(BakingSystemGroup), OrderFirst = true)]
    public partial class BakingSettingsSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            if (!SystemAPI.TryGetSingletonRW<UnityBakingSettings>(out var unityBakingSettings))
            {
                var entity = this.EntityManager.CreateEntity(typeof(BakingOnlyEntity), typeof(UnityBakingSettings));
                unityBakingSettings = SystemAPI.GetComponentRW<UnityBakingSettings>(entity);
            }

            var bs = this.World.GetExistingSystemManaged<BakingSystem>().BakingSettings;
            ref var ubs = ref unityBakingSettings.ValueRW;
            ubs.SceneGUID = bs.SceneGUID;
            ubs.BakingFlags = (BakingFlags)bs.BakingFlags;
            ubs.FilterFlags = bs.FilterFlags;
        }
    }
}
