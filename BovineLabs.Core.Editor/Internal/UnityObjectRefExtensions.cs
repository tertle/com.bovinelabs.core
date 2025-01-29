// <copyright file="UnityObjectRefExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Internal
{
    using BovineLabs.Core.Internal;
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
            var meshGoid = GlobalObjectId.GetGlobalObjectIdSlow(unityObjectRef.GetInstanceId());
            var meshRtgoid = UnsafeUtility.As<GlobalObjectId, RuntimeGlobalObjectId>(ref meshGoid);
            return new UntypedWeakReferenceId(meshRtgoid, WeakReferenceGenerationType.UnityObject);
        }
    }
}
