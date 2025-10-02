// <copyright file="InitializeSystemGroup.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_LIFECYCLE
namespace BovineLabs.Core.LifeCycle
{
    using BovineLabs.Core.Groups;
    using BovineLabs.Core.Utility;
    using Unity.Entities;
#if !BL_DISABLE_PAUSE
    using BovineLabs.Core.Pause;
#endif

    /// <summary>
    /// Handles initialization of entities. Updates before DestroySystemGroup to allow access to data from destroyed entities.
    /// </summary>
    [UpdateAfter(typeof(InstantiateCommandBufferSystem))]
    [UpdateBefore(typeof(DestroySystemGroup))]
    [UpdateInGroup(typeof(BeforeSceneSystemGroup))]
    public partial class InitializeSystemGroup : ComponentSystemGroup
    {
        /// <inheritdoc />
        protected override void OnUpdate()
        {
#if !BL_DISABLE_PAUSE
            foreach (var p in SystemAPI.Query<RefRO<PauseGame>>().WithOptions(EntityQueryOptions.IncludeSystems))
            {
                if (p.ValueRO.PauseAll)
                {
                    return;
                }
            }
#endif

            var query = SystemAPI.QueryBuilder().WithAny<InitializeEntity, InitializeSubSceneEntity>().Build();
            if (BurstUtil.IsEmpty(ref query))
            {
                return;
            }

            base.OnUpdate();
        }
    }
}
#endif
