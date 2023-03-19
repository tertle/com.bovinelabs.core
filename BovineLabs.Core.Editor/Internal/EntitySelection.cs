// <copyright file="EntitySelection.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Internal
{
    using System.Collections.Generic;
    using Unity.Entities;
    using Unity.Entities.Editor;
    using UnityEditor;

    public static class EntitySelection
    {
        public static bool IsSelected => Selection.activeObject is EntitySelectionProxy;

        public static World World => ((EntitySelectionProxy)Selection.activeObject).World;

        public static Entity Entity => ((EntitySelectionProxy)Selection.activeObject).Entity;

        public static IEnumerable<(World World, Entity Entity)> GetAllSelections()
        {
            foreach (var s in Selection.objects)
            {
                if (s is EntitySelectionProxy proxy)
                {
                    yield return (proxy.World, proxy.Entity);
                }
            }
        }

        public static void UnSelect()
        {
            if (Selection.activeObject is EntitySelectionProxy)
            {
                Selection.activeObject = null;
            }
        }
    }
}
