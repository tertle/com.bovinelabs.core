// <copyright file="WeakObjectReferenceExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Internal
{
    using Unity.Entities.Content;
    using Unity.Entities.Serialization;
    using UnityEngine;

    public static class WeakObjectReferenceExtensions
    {
        public static UntypedWeakReferenceId GetId<T>(this WeakObjectReference<T> weakObjectReference)
            where T : Object
        {
            return weakObjectReference.Id;
        }

        public static void SetId<T>(ref this WeakObjectReference<T> weakObjectReference, UntypedWeakReferenceId id)
            where T : Object
        {
            weakObjectReference.Id = id;
        }
    }
}
