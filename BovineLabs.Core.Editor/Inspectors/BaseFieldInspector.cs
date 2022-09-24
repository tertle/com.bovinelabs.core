namespace BovineLabs.Core.Editor.Inspectors
{
    using Unity.Properties.UI;
    using UnityEngine.UIElements;

    // Copy from Unity.Entities.Editor.Inspectors
    public abstract class BaseFieldInspector<TField, TFieldValue, TValue> : Inspector<TValue>
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
            return this.m_Field;
        }
    }
}
