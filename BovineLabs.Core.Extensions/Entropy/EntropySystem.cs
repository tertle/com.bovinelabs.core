// <copyright file="EntropySystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_ENTROPY
namespace BovineLabs.Core.Entropy
{
    using BovineLabs.Core.Collections;
    using Unity.Collections;
    using Unity.Entities;

    public partial struct EntropySystem : ISystem
    {
        private ThreadRandom threadRandom;

        public void OnCreate(ref SystemState state)
        {
            state.Enabled = false;
            this.threadRandom = new ThreadRandom((uint)UnityEngine.Random.Range(0, int.MaxValue), Allocator.Persistent);
            state.EntityManager.AddComponentData(state.SystemHandle, new Entropy { Random = this.threadRandom });
        }

        public void OnDestroy(ref SystemState state)
        {
            this.threadRandom.Dispose();
        }
    }
}
#endif
