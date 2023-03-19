// <copyright file="BaseFieldInspector.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Inspectors
{
    using Unity.Entities.Editor;
    using Unity.Entities.UI;
    using UnityEngine.UIElements;

    // Copy from Unity.Entities.Editor.Inspectors
    internal abstract class BaseFieldInspector<TField, TFieldValue, TValue> : PropertyInspector<TValue>
        where TField : BaseField<TFieldValue>, new()
    {
        public override VisualElement Build()
        {
            var field = new TField
            {
                name = this.Name,
                label = this.DisplayName,
                tooltip = this.Tooltip,
                bindingPath = ".",
            };

            InspectorUtility.AddRuntimeBar(field);
            return field;
        }
    }
}
