// <copyright file="UnityObjectRefExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Internal
{
    using Unity.Entities;
    using UnityEngine;

    public static class UnityObjectRefExtensions
    {
        public static EntityId GetInstanceId<T>(this UnityObjectRef<T> unityObjectRef)
            where T : Object
        {
#if UNITY_6000_4_OR_NEWER
            return unityObjectRef.Id.entityId;
#else
            return unityObjectRef.Id.instanceId;
#endif
        }

        public static void SetInstanceId<T>(this ref UnityObjectRef<T> unityObjectRef, EntityId entityId)
            where T : Object
        {
#if UNITY_6000_4_OR_NEWER
            unityObjectRef.Id.entityId = entityId;
#else
            unityObjectRef.Id.instanceId = entityId;
#endif
        }
    }
}
