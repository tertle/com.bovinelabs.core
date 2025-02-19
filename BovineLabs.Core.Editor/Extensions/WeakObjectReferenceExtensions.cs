// <copyright file="WeakObjectReferenceExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Extensions
{
    using Unity.Entities.Content;
    using Unity.Entities.Serialization;
    using UnityEngine;

    public static class WeakObjectReferenceExtensions
    {
        public static T? GetEditorObject<T>(this WeakObjectReference<T> weakObjectReference)
            where T : Object
        {
            return UntypedWeakReferenceId.GetEditorObject(weakObjectReference.Id) as T;
        }
    }
}
