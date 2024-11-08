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
    using UnityEngine;

    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class EntropySystem : SystemBase
    {
        private ThreadRandom threadRandom;

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            this.threadRandom = new ThreadRandom((uint)Random.Range(0, int.MaxValue), Allocator.Persistent);
            this.EntityManager.AddComponentData(this.SystemHandle, new Entropy { Random = this.threadRandom });
        }

        /// <inheritdoc/>
        protected override void OnDestroy()
        {
            this.threadRandom.Dispose();
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            this.World.GetExistingSystemManaged<InitializationSystemGroup>().RemoveSystemFromUpdateList(this);
        }
    }
}
#endif
