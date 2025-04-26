// <copyright file="DestroySystemGroup.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_LIFECYCLE
namespace BovineLabs.Core.LifeCycle
{
    using BovineLabs.Core.Groups;
    using BovineLabs.Core.Utility;
    using Unity.Burst;
    using Unity.Entities;

    [BurstCompile]
    [UpdateInGroup(typeof(BeforeSceneSystemGroup))]
    [UpdateAfter(typeof(InstantiateCommandBufferSystem))]
    public partial class DestroySystemGroup : ComponentSystemGroup
    {
        protected override void OnUpdate()
        {
            var query = SystemAPI.QueryBuilder().WithAll<DestroyEntity>().Build();
            if (BurstUtil.IsEmpty(ref query))
            {
                return;
            }

            base.OnUpdate();
        }
    }
}
#endif
