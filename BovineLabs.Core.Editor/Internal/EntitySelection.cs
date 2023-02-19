// <copyright file="EntitySelection.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Internal
{
    using Unity.Entities;
    using Unity.Entities.Editor;
    using UnityEditor;

    public static class EntitySelection
    {
        public static bool IsSelected => Selection.activeObject is EntitySelectionProxy;

        public static World World => ((EntitySelectionProxy)Selection.activeObject).World;

        public static Entity Entity => ((EntitySelectionProxy)Selection.activeObject).Entity;

        public static void UnSelect()
        {
            if (Selection.activeObject is EntitySelectionProxy)
            {
                Selection.activeObject = null;
            }
        }
    }
}
