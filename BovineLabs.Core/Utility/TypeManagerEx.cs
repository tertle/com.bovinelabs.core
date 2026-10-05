namespace BovineLabs.Core.Utility
{
    using System.Collections.Generic;
    using System.Reflection;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
    using Unity.Scripting.LifecycleManagement;

    /// <summary>Code-lifetime, Burst-readable metadata for registered query-group components.</summary>
    public static unsafe partial class TypeManagerEx
    {
        [NoAutoStaticsCleanup]
        private static readonly SharedStatic<GroupData> Groups = SharedStatic<GroupData>.GetOrCreate<TypeManagerExContext>();

        /// <summary>Initializes metadata on the main thread after Entities has registered its component types.</summary>
        [OnCodeLoaded]
        public static void Initialize()
        {
            if (Groups.Data.Ranges.IsCreated)
            {
                return;
            }

            TypeManager.Initialize();

            var groupsByTarget = new Dictionary<int, HashSet<TypeIndex>>();
            foreach (var typeInfo in TypeManager.AllTypes)
            {
                if (typeInfo.Type == null)
                {
                    continue;
                }

                foreach (var attribute in typeInfo.Type.GetCustomAttributes<QueryGroupAttribute>())
                {
                    var targetIndex = TypeManager.GetTypeIndex(attribute.TargetType).Index;
                    if (!groupsByTarget.TryGetValue(targetIndex, out var members))
                    {
                        members = new HashSet<TypeIndex>();
                        groupsByTarget.Add(targetIndex, members);
                    }

                    members.Add(typeInfo.TypeIndex);
                }
            }

            var data = new GroupData
            {
                Ranges = new UnsafeList<GroupRange>(TypeManager.GetTypeCount(), Allocator.Persistent),
                Members = new UnsafeList<TypeIndex>(0, Allocator.Persistent),
            };
            data.Ranges.Resize(TypeManager.GetTypeCount(), NativeArrayOptions.ClearMemory);

            // Follow type-index order so enumeration is stable regardless of reflection/attribute order.
            foreach (var typeInfo in TypeManager.AllTypes)
            {
                if (!groupsByTarget.TryGetValue(typeInfo.TypeIndex.Index, out var members))
                {
                    continue;
                }

                var sortedMembers = new List<TypeIndex>(members);
                sortedMembers.Sort((left, right) => left.Index.CompareTo(right.Index));
                var range = new GroupRange
                {
                    Start = data.Members.Length,
                    Count = sortedMembers.Count,
                };
                foreach (var member in sortedMembers)
                {
                    data.Members.Add(member);
                }

                data.Ranges[typeInfo.TypeIndex.Index] = range;
            }

            Groups.Data = data;
        }

        public static int GetQueryGroupCount(TypeIndex typeIndex)
        {
            return Groups.Data.Ranges[typeIndex.Index].Count;
        }

        /// <summary>Returns borrowed, flagged type indices valid until code unloading. Use GetQueryGroupCount for the length.</summary>
        public static TypeIndex* GetQueryGroups(TypeIndex typeIndex)
        {
            var range = Groups.Data.Ranges[typeIndex.Index];
            return range.Count == 0 ? null : Groups.Data.Members.Ptr + range.Start;
        }

        [OnCodeUnloading]
        private static void Shutdown()
        {
            if (!Groups.Data.Ranges.IsCreated)
            {
                return;
            }

            Groups.Data.Ranges.Dispose();
            Groups.Data.Members.Dispose();
            Groups.Data = default;
        }

        private struct GroupData
        {
            public UnsafeList<GroupRange> Ranges;
            public UnsafeList<TypeIndex> Members;
        }

        private struct GroupRange
        {
            public int Start;
            public int Count;
        }

        private struct TypeManagerExContext
        {
        }
    }
}
