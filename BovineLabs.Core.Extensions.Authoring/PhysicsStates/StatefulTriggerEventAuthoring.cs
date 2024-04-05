// <copyright file="StatefulTriggerEventAuthoring.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_PHYSICS_STATES
namespace BovineLabs.Core.Authoring.PhysicsStates
{
    using BovineLabs.Core.PhysicsStates;
    using Unity.Entities;
    using UnityEngine;

    public class StatefulTriggerEventAuthoring : MonoBehaviour
    {
        private class Baker : Baker<StatefulTriggerEventAuthoring>
        {
            public override void Bake(StatefulTriggerEventAuthoring authoring)
            {
                var entity = this.GetEntity(TransformUsageFlags.None);
                this.AddBuffer<StatefulTriggerEvent>(entity);
            }
        }
    }
}
#endif
