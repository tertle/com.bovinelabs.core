// <copyright file="InitializeEntitySystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.LifeCycle
{
    using Unity.Burst;
    using Unity.Entities;

    [UpdateInGroup(typeof(InitializeSystemGroup), OrderLast = true)]
    public partial struct InitializeEntitySystem : ISystem
    {
        /// <inheritdoc/>
        public void OnUpdate(ref SystemState state)
        {
            new InstantiateJob().ScheduleParallel();
        }

        [BurstCompile]
        private partial struct InstantiateJob : IJobEntity
        {
            private static void Execute(EnabledRefRW<InitializeEntity> instantiateEntity)
            {
                instantiateEntity.ValueRW = false;
            }
        }
    }
}
