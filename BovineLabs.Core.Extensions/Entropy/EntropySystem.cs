// <copyright file="EntropySystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_ENTROPY
namespace BovineLabs.Core.Entropy
{
    using BovineLabs.Core.Collections;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;

    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct EntropySystem : ISystem
    {
        private ThreadRandom threadRandom;

        /// <inheritdoc/>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.Enabled = false;
            this.threadRandom = new ThreadRandom((uint)UnityEngine.Random.Range(0, int.MaxValue), Allocator.Persistent);
            state.EntityManager.AddComponentData(state.SystemHandle, new Entropy { Random = this.threadRandom });
        }

        /// <inheritdoc/>
        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            this.threadRandom.Dispose();
        }

        /// <inheritdoc/>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // NO-OP
        }
    }
}
#endif
