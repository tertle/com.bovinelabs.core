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
                name = Name,
                label = DisplayName,
                tooltip = Tooltip,
                bindingPath = ".",
            };

            InspectorUtility.AddRuntimeBar(field);
            return field;
        }
    }
}
