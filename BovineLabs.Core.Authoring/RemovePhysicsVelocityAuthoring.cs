// <copyright file="RemovePhysicsVelocityAuthoring.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_PHYSICS
namespace BovineLabs.Core.Authoring
{
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Physics;
    using UnityEngine;

    public class RemovePhysicsVelocityAuthoring : MonoBehaviour
    {
    }

    [TemporaryBakingType]
    internal struct RemovePhysicsVelocityBaking : IComponentData
    {
    }

    public class RemovePhysicsVelocityBaker : Baker<RemovePhysicsVelocityAuthoring>
    {
        public override void Bake(RemovePhysicsVelocityAuthoring authoring)
        {
            this.AddComponent<RemovePhysicsVelocityBaking>();
        }
    }

    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    public partial struct RemovePhysicsVelocityConversionSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (_, entity) in SystemAPI.Query<RemovePhysicsVelocityBaking>().WithAll<PhysicsVelocity>().WithEntityAccess())
            {
                ecb.RemoveComponent<PhysicsVelocity>(entity);
            }

            ecb.Playback(state.EntityManager);
        }
    }
}
#endif
