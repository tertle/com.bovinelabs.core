// <copyright file="GameObjectExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Extensions
{
    using UnityEngine;

    public static class GameObjectExtensions
    {
        public static bool IsPrefab(this GameObject go)
        {
            return !go.scene.IsValid();
        }

        public static bool IsAsset(this Object o)
        {
            return o is not GameObject && o is not Component;
        }
    }
}
