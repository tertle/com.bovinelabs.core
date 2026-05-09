// <copyright file="UnityObjectRefExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Internal
{
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
    using Unity.Entities.Serialization;
    using UnityEditor;
    using UnityEngine;

    public static class UnityObjectRefExtensions
    {
        public static UntypedWeakReferenceId ToUntypedWeakReferenceId<T>(this UnityObjectRef<T> unityObjectRef)
            where T : Object
        {
#if UNITY_6000_4_OR_NEWER
            var guid = GlobalObjectId.GetGlobalObjectIdSlow(unityObjectRef.Id.entityId);
#else
            var guid = GlobalObjectId.GetGlobalObjectIdSlow((EntityId)unityObjectRef.Id.instanceId);
#endif

            var rgGuid = UnsafeUtility.As<GlobalObjectId, RuntimeGlobalObjectId>(ref guid);
            return new UntypedWeakReferenceId(rgGuid, WeakReferenceGenerationType.UnityObject);
        }
    }
}
