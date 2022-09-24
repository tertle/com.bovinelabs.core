// <copyright file="RemovePhysicsVelocityConversionSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_PHYSICS
namespace BovineLabs.Core.Authoring
{
    using Unity.Entities;
    using Unity.Physics;
    using Unity.Physics.Authoring;

    [UpdateAfter(typeof(EndColliderConversionSystem))]
    public class RemovePhysicsVelocityConversionSystem : GameObjectConversionSystem
    {
        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            this.Entities.ForEach((RemovePhysicsVelocity component) =>
            {
                var entity = this.GetPrimaryEntity(component);

                if (this.DstEntityManager.HasComponent<PhysicsVelocity>(entity))
                {
                    this.DstEntityManager.RemoveComponent<PhysicsVelocity>(entity);
                }
            });
        }
    }
}
#endif
