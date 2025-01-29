// <copyright file="EntityStorageInfoLookupExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Extensions
{
    using System.Diagnostics.CodeAnalysis;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    public static unsafe class EntityStorageInfoLookupExtensions
    {
        public static void GetNameUnsafe(this EntityStorageInfoLookup lookup, Entity entity, out FixedString64Bytes name)
        {
            ref var access = ref UnsafeUtility.As<EntityStorageInfoLookup, EntityStorageInfoLookupAccess>(ref lookup);
            access.EntityDataAccess->GetName(entity, out name);
        }

        [SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1214:Readonly fields should appear before non-readonly fields", Justification = "Memory")]
        private struct EntityStorageInfoLookupAccess
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            internal AtomicSafetyHandle Safety;
#endif
            [NativeDisableUnsafePtrRestriction]
            internal readonly EntityDataAccess* EntityDataAccess;
        }
    }
}
