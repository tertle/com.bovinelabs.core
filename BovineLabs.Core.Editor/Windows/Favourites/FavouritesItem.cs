// <copyright file="FavouritesItem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Windows.Favourites
{
    using System;
    using BovineLabs.Core.Editor.Windows.Base;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// Represents a single item in the favourites collection.
    /// </summary>
    public sealed class FavouritesItem : BaseObjectItem
    {
        public FavouritesItem(UnityEngine.Object obj)
            : base(obj, GlobalObjectId.GetGlobalObjectIdSlow(obj))
        {
        }

        public FavouritesItem(
            UnityEngine.Object? obj, string name, string typeName, string assetPath, GlobalObjectId globalObjectId, Texture2D? icon, DateTime timestamp)
            : base(obj, name, typeName, assetPath, globalObjectId, icon, timestamp)
        {
        }
    }
}