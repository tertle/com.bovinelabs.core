// <copyright file="InspectorUtility.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Internal
{
    using UnityEngine.UIElements;

    public static class InspectorUtility
    {
        public static void AddRuntimeBar(VisualElement parent)
        {
            Unity.Entities.Editor.InspectorUtility.AddRuntimeBar(parent);
        }
    }
}
