// <copyright file="EntityDestroy.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Destroy
{
    using Unity.Entities;

    /// <summary>
    /// Unified destroy component allowing entities to all pass through a singular cleanup group.
    /// </summary>
    [ChangeFilterTracking]
    public struct EntityDestroy : IComponentData
    {
        public static readonly EntityDestroy Reset = new() { Value = 0 };
        public static readonly EntityDestroy Destroy = new() { Value = 1 };
        public static readonly EntityDestroy CancelDestroy = new() { Value = -1 };

        internal sbyte Value;

        public bool IsDestroyed => this.Value != 0;
    }
}
