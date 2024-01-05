// <copyright file="HybridSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_HYBRID
namespace BovineLabs.Core.Hybrid
{
    using BovineLabs.Core.Groups;
    using Unity.Entities;
    using UnityEngine;

    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(AfterSceneSystemGroup))]
    public partial class HybridSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            foreach (var (hybrid, entity) in SystemAPI.Query<HybridComponent>().WithNone<Transform>().WithEntityAccess())
            {
                this.EntityManager.AddComponentObject(entity, hybrid.Value!.transform);
            }
        }
    }
}
#endif
