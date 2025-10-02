// <copyright file="PropertyInspector.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Internal
{
    using Unity.Entities.UI;
    using UnityEngine.UIElements;

    public static class PropertyInspector
    {
        public static VisualElement Make(object target)
        {
            return PropertyElement.MakeWithValue(target);
        }
    }
}
