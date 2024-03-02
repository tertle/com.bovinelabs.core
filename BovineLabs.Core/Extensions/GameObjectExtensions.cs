// <copyright file="GameObjectExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Extensions
{
    using System.Runtime.CompilerServices;
    using UnityEngine;

    public static class GameObjectExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPrefab(this GameObject go)
        {
            return !go.scene.IsValid();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPrefab(this Component comp)
        {
            return comp.gameObject.IsPrefab();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAsset(this Object o)
        {
            return o is not GameObject && o is not Component;
        }
    }
}
