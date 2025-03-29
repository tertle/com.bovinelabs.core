// <copyright file="WorldUnmanagedExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Extensions
{
    using JetBrains.Annotations;
    using Unity.Burst.CompilerServices;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Jobs;
    using Unity.Jobs.LowLevel.Unsafe;
    using Unity.Profiling;

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

        public static unsafe JobHandle GetTrackedJobHandle(this WorldUnmanaged world)
        {
            var dm = (ComponentDependencyManagerClone*)world.EntityManager.GetCheckedEntityDataAccess()->DependencyManager;

            if (dm->DependencyHandlesCount != 0)
            {
                return GetCombinedDependencyForAllTypes(dm);
            }

            return default;
        }

        private static unsafe JobHandle GetCombinedDependencyForAllTypes(ComponentDependencyManagerClone* dm)
        {
            var allHandles = stackalloc JobHandle[dm->DependencyHandlesCount *
                (ComponentDependencyManagerClone.MaxReadJobHandles + ComponentDependencyManagerClone.MaxWriteJobHandles)];

            var allHandleCount = 0;
            for (var i = 0; i != dm->DependencyHandlesCount; i++)
            {
                allHandles[allHandleCount++] = dm->DependencyHandles[i].WriteFence;
                var readHandleCount = dm->DependencyHandles[i].NumReadFences;
                var readHandles = dm->ReadJobFences + (i * ComponentDependencyManagerClone.MaxReadJobHandles);
                for (var j = 0; j < readHandleCount; ++j)
                {
                    allHandles[allHandleCount++] = readHandles[j];
                }
            }

            if (Hint.Unlikely(allHandleCount == 0))
            {
                return default;
            }

            if (allHandleCount == 1)
            {
                return allHandles[0];
            }

            return JobHandleUnsafeUtility.CombineDependencies(allHandles, allHandleCount);
        }

        // Must match ComponentDependencyManager
        [UsedImplicitly]
        internal unsafe struct ComponentDependencyManagerClone
        {
            public const int MaxWriteJobHandles = 1;
            public const int MaxReadJobHandles = 17;

            // Indexed by TypeIndex
            public ushort* TypeArrayIndices;
            public DependencyHandle* DependencyHandles;
            public ushort DependencyHandlesCount;
            public JobHandle* ReadJobFences;

            public TypeIndex EntityTypeIndex;

            public JobHandle ExclusiveTransactionDependency;
            public byte IsInTransaction;

            public ProfilerMarker Marker;
            public WorldUnmanaged World;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            public ComponentSafetyHandles Safety;
#endif
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
            public ForEachDisallowStructuralChangeSupport ForEachStructuralChange;
#endif

            public struct DependencyHandle
            {
                public JobHandle WriteFence;
                public int NumReadFences;
                public TypeIndex TypeIndex;
            }
        }
    }
}
