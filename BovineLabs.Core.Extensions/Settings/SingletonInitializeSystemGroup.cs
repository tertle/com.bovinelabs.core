// <copyright file="InitializeSingletonSystemGroup.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Settings
{
    using BovineLabs.Core.Groups;
    using BovineLabs.Core.LifeCycle;
    using BovineLabs.Core.Pause;
    using BovineLabs.Core.Utility;
    using Unity.Entities;

#if !BL_DISABLE_LIFECYCLE
    [UpdateBefore(typeof(InitializeSystemGroup))]
#endif
    [UpdateInGroup(typeof(BeginSimulationSystemGroup), OrderFirst = true)]
    public partial class SingletonInitializeSystemGroup : ComponentSystemGroup, IUpdateWhilePaused
    {
        /// <inheritdoc />
        protected override void OnUpdate()
        {
            var query = SystemAPI.QueryBuilder().WithAll<SingletonInitialize>().Build();
            if (BurstUtil.IsEmpty(ref query))
            {
                return;
            }

            base.OnUpdate();
        }
    }
}
