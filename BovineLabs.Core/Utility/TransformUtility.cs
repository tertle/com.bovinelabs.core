// <copyright file="TransformUtility.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Utility
{
    using BovineLabs.Core.EntityCommands;
    using Unity.Entities;
    using Unity.Mathematics;
    using Unity.Transforms;

    public static class TransformUtility
    {
        public static void SetupParent<T>(
            ref T commands, Entity parent, Entity child, in LocalToWorld parentLocalToWorld, in LocalTransform childLocalTransform, DynamicBuffer<Child> childs)
            where T : IEntityCommands
        {
            // Setup the child
            commands.Entity = child;
            commands.AddComponent(new ComponentTypeSet(ComponentType.ReadWrite<Parent>(), ComponentType.ReadWrite<PreviousParent>()));
            commands.SetComponent(new Parent { Value = parent });
            commands.SetComponent(new PreviousParent { Value = parent });
            commands.SetComponent(new LocalToWorld { Value = math.mul(parentLocalToWorld.Value, childLocalTransform.ToMatrix()) });

            // Setup the parent
            commands.Entity = parent;
            if (childs.IsCreated)
            {
                commands.AppendToBuffer(new Child { Value = child });
            }
            else
            {
                commands.AddBuffer<Child>().Add(new Child { Value = child });
            }
        }
    }
}
