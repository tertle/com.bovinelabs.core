// <copyright file="TypeManagerOverrideSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Utility
{
    using Unity.Entities;
    using Unity.Entities.Hybrid.Baking;

    // Baking initialization
    [CreateBefore(typeof(LinkedEntityGroupBakingCleanUp))]
    [UpdateInGroup(typeof(PreBakingSystemGroup), OrderFirst = true)]
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    public partial struct TypeManagerOverrideSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            TypeManagerOverrides.Initialize();
        }
    }
}
