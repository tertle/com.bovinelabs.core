// <copyright file="BufferCapacityAttribute.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Utility
{
    using System;
    using Unity.Entities;
    using Unity.Entities.Hybrid.Baking;

    /// <summary> Assembly attribute that can be used to override <see cref="InternalBufferCapacityAttribute" /> or default values. </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class BufferCapacityAttribute : Attribute
    {
        public readonly int Capacity;
        public readonly Type Type;

        public BufferCapacityAttribute(Type type, int capacity = 0)
        {
            this.Type = type;
            this.Capacity = capacity;
        }
    }

    // Editor initialization

    // Baking initialization
    [CreateBefore(typeof(LinkedEntityGroupBakingCleanUp))]
    [UpdateInGroup(typeof(PreBakingSystemGroup), OrderFirst = true)]
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    public struct ResizeBufferSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            ResizeBufferCapacity.Initialize();
        }
    }
}
