namespace BovineLabs.Core.UI
{
    using System.Collections.Generic;
    using UnityEngine.UIElements;

    [UxmlElement]
    public partial class BovineThemeSelector : VisualElement
    {
        private readonly DropdownField _field;

        public BovineThemeSelector()
        {
            AddToClassList("bl-theme-selector");
            _field = new DropdownField("Theme", new List<string> { "Bovine Works", "The Curator" }, (int)BovineThemeUtility.Theme);
            _field.RegisterValueChangedCallback(evt => BovineThemeUtility.Theme = (BovineTheme)_field.index);
            Add(_field);
            RegisterCallback<AttachToPanelEvent>(OnAttach);
            RegisterCallback<DetachFromPanelEvent>(OnDetach);
        }

        private void OnAttach(AttachToPanelEvent evt)
        {
            BovineThemeUtility.ThemeChanged += OnThemeChanged;
            OnThemeChanged(BovineThemeUtility.Theme);
        }

        private void OnDetach(DetachFromPanelEvent evt)
        {
            BovineThemeUtility.ThemeChanged -= OnThemeChanged;
        }

        private void OnThemeChanged(BovineTheme theme)
        {
            _field.SetValueWithoutNotify(_field.choices[(int)theme]);
        }
    }
}
