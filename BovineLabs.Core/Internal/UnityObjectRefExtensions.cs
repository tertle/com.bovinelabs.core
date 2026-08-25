// <copyright file="UnityObjectRefExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Internal
{
    using Unity.Entities;
    using UnityEngine;

    public static class UnityObjectRefExtensions
    {
        public static EntityId GetEntityId<T>(this UnityObjectRef<T> unityObjectRef)
            where T : Object
        {
            return unityObjectRef.Id.entityId;
        }

        public static void SetEntityId<T>(this ref UnityObjectRef<T> unityObjectRef, EntityId entityId)
            where T : Object
        {
            unityObjectRef.Id.entityId = entityId;
        }
    }
}
