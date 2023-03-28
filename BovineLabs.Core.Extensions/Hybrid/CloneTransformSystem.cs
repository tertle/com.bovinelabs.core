// <copyright file="CloneTransformSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Hybrid
{
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
    using Unity.Transforms;

    [UpdateInGroup(typeof(TransformSystemGroup), OrderFirst = true)]
    public partial struct CloneTransformSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            new CloneTransformJob { LocalTransforms = SystemAPI.GetComponentLookup<LocalTransform>(true) }.Schedule();
        }

        [BurstCompile]
        private partial struct CloneTransformJob : IJobEntity
        {
            [ReadOnly]
            [NativeDisableContainerSafetyRestriction]
            public ComponentLookup<LocalTransform> LocalTransforms;

            private void Execute(ref LocalTransform transform, in CloneTransform cloneTransform)
            {
                if (this.LocalTransforms.TryGetComponent(cloneTransform.Value, out var copy))
                {
                    transform = copy;
                }
            }
        }
    }
}
