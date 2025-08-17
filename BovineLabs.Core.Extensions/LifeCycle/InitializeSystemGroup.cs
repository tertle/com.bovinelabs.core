// <copyright file="InitializeSystemGroup.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_LIFECYCLE
namespace BovineLabs.Core.LifeCycle
{
    using BovineLabs.Core.Groups;
    using BovineLabs.Core.Pause;
    using BovineLabs.Core.Utility;
    using Unity.Entities;

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
