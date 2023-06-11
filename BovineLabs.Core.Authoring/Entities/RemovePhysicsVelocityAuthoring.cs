// <copyright file="RemovePhysicsVelocityAuthoring.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_PHYSICS
namespace BovineLabs.Core.Authoring.Entities
{
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Physics;
    using UnityEngine;

    public partial class RemovePhysicsVelocityAuthoring : MonoBehaviour
    {
        [TemporaryBakingType]
        internal struct RemovePhysicsVelocityBaking : IComponentData
        {
        }

        public class Baker : Baker<RemovePhysicsVelocityAuthoring>
        {
            public override void Bake(RemovePhysicsVelocityAuthoring authoring)
            {
                var entity = this.GetEntity(TransformUsageFlags.None);
                this.AddComponent<RemovePhysicsVelocityBaking>(entity);
            }
        }

        [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
        public partial struct RemovePhysicsVelocityConversionSystem : ISystem
        {
            [BurstCompile]
            public void OnUpdate(ref SystemState state)
            {
                var query = SystemAPI.QueryBuilder().WithAll<RemovePhysicsVelocityBaking, PhysicsVelocity>().WithOptions(EntityQueryOptions.IncludePrefab).Build();
                state.EntityManager.RemoveComponent<PhysicsVelocity>(query);
            }
        }
    }
}
#endif
