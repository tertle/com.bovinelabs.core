namespace BovineLabs.Core.UI
{
    using System.Collections.Generic;
    using UnityEngine.UIElements;

    [UxmlElement]
    public partial class BovineThemeSelector : VisualElement
    {
        private readonly DropdownField field;

        public BovineThemeSelector()
        {
            this.AddToClassList("bl-theme-selector");
            this.field = new DropdownField("Theme", new List<string> { "Bovine Works", "The Curator" }, (int)BovineThemeUtility.Theme);
            this.field.RegisterValueChangedCallback(evt => BovineThemeUtility.Theme = (BovineTheme)this.field.index);
            this.Add(this.field);
            this.RegisterCallback<AttachToPanelEvent>(this.OnAttach);
            this.RegisterCallback<DetachFromPanelEvent>(this.OnDetach);
        }

        private void OnAttach(AttachToPanelEvent evt)
        {
            BovineThemeUtility.ThemeChanged += this.OnThemeChanged;
            this.OnThemeChanged(BovineThemeUtility.Theme);
        }

        private void OnDetach(DetachFromPanelEvent evt)
        {
            BovineThemeUtility.ThemeChanged -= this.OnThemeChanged;
        }

        private void OnThemeChanged(BovineTheme theme)
        {
            this.field.SetValueWithoutNotify(this.field.choices[(int)theme]);
        }
    }
}
