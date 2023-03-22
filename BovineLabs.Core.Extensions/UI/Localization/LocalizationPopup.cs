// <copyright file="LocalizationPopup.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_UI && UNITY_LOCALIZATION
namespace BovineLabs.Core.UI.Localization
{
    using System.Linq;
    using UnityEngine.Localization;
    using UnityEngine.Localization.Settings;
    using UnityEngine.Scripting;
    using UnityEngine.UIElements;

    [Preserve]
    public class LocalizationPopup : VisualElement
    {
        private readonly PopupField localesPopup;

        public LocalizationPopup()
        {
            this.localesPopup = new PopupField();
            this.Add(this.localesPopup);

            if (LocalizationSettings.HasSettings)
            {
                LocalizationSettings.InitializationOperation.Completed += _ =>
                {
                    this.localesPopup.SetDisplayNames(LocalizationSettings.AvailableLocales.Locales.Select(s => s.ToString()));
                    this.localesPopup.SetValueWithoutNotify(LocalizationSettings.AvailableLocales.Locales.IndexOf(LocalizationSettings.SelectedLocale));
                    this.localesPopup.RegisterValueChangedCallback(OnValueChanged);

                    LocalizationSettings.SelectedLocaleChanged += this.OnSelectedLocaleChanged;
                };
            }
        }

        private static void OnValueChanged(ChangeEvent<int> evt)
        {
            LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[evt.newValue];
        }

        private void OnSelectedLocaleChanged(Locale obj)
        {
            this.localesPopup.SetValueWithoutNotify(LocalizationSettings.AvailableLocales.Locales.IndexOf(LocalizationSettings.SelectedLocale));
        }

        /// <summary> The factory for UI builder support. </summary>
        [Preserve]
        public new class UxmlFactory : UxmlFactory<LocalizationPopup, UxmlFactory.LocalizationPopupUxmlTraits>
        {
            public class LocalizationPopupUxmlTraits : UxmlTraits
            {
            }
        }
    }
}
#endif
