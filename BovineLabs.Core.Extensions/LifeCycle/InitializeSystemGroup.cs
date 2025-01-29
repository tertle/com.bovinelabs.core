// <copyright file="InitializeSystemGroup.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_LIFECYCLE
namespace BovineLabs.Core.LifeCycle
{
    using BovineLabs.Core.Groups;
    using BovineLabs.Core.SubScenes;
    using Unity.Entities;

    [UpdateInGroup(typeof(BeginSimulationSystemGroup), OrderFirst = true)]
    public partial class InitializeSystemGroup : AlwaysUpdateSystemGroup
    {
        /// <inheritdoc />
        protected override void OnUpdate()
        {
            var query = SystemAPI.QueryBuilder().WithAny<InitializeEntity, InitializeSubSceneEntity>().Build();
            if (query.IsEmpty)
            {
                return;
            }

            base.OnUpdate();
        }
    }
}
#endif
