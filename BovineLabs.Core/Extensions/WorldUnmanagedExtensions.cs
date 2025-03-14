// <copyright file="WorldUnmanagedExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Extensions
{
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Jobs;

    public static class WorldUnmanagedExtensions
    {
        public static bool SystemExists<T>(this WorldUnmanaged world)
        {
            var typeIndex = TypeManager.GetSystemTypeIndex<T>();
            return world.GetExistingUnmanagedSystem(typeIndex) != SystemHandle.Null;
        }

        public static unsafe void GetAllSystemDependencies(this WorldUnmanaged world, NativeList<JobHandle> dependencies)
        {
            using var e = world.GetImpl().m_SystemStatePtrMap.GetEnumerator();
            while (e.MoveNext())
            {
                var systemState = (SystemState*)e.Current.Value;
                dependencies.Add(systemState->m_JobHandle);
            }
        }
    }
}
