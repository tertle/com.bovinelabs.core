namespace BovineLabs.Core.Editor.Inspectors
{
    using Unity.Entities.Editor;
    using Unity.Platforms.UI;
    using UnityEngine.UIElements;

    // Copy from Unity.Entities.Editor.Inspectors
    internal abstract class BaseFieldInspector<TField, TFieldValue, TValue> : PropertyInspector<TValue>
        where TField : BaseField<TFieldValue>, new()
    {
        protected TField m_Field;

        public override VisualElement Build()
        {
            this.m_Field = new TField
            {
                name = this.Name,
                label = this.DisplayName,
                tooltip = this.Tooltip,
                bindingPath = "."
            };

            InspectorUtility.AddRuntimeBar(m_Field);
            return this.m_Field;
        }
    }
}
