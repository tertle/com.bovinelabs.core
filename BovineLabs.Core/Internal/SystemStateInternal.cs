// <copyright file="SystemStateInternal.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Internal
{
    using Unity.Entities;

    public static class SystemStateInternal
    {
        public static ComponentSystemBase GetManagedSystem(this SystemState state)
        {
            return state.ManagedSystem;
        }

        public static string GetSystemTypeName(this SystemState state)
        {
            var managed = state.GetManagedSystem();
            return managed != null ? TypeManager.GetSystemName(managed.GetType()).ToString() : state.DebugName.ToString();
        }
    }
}
