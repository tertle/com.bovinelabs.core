// <copyright file="LocalizationToolbarBindings.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_TOOLBAR && UNITY_LOCALIZATION
namespace BovineLabs.Core.ToolbarTabs
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using BovineLabs.Core.UI;
    using Unity.Properties;
    using UnityEngine.Localization;
    using UnityEngine.Localization.Settings;
    using UnityEngine.UIElements;

    public class LocalizationToolbarBindings : IBindingObject<LocalizationToolbarBindings.Data>, INotifyBindablePropertyChanged, IDisposable
    {
        private Data data;
        private int selectedLocale = -1;

        public LocalizationToolbarBindings()
        {
            if (!LocalizationSettings.HasSettings)
            {
                return;
            }

            LocalizationSettings.InitializationOperation.Completed += _ =>
            {
                this.Locales.Clear();
                this.Locales.AddRange(LocalizationSettings.AvailableLocales.Locales.Select(s => s.ToString()));
                this.Notify(nameof(this.Locales));

                var locale = LocalizationSettings.SelectedLocale;
                this.SelectedLocale = locale != null ? this.Locales.IndexOf(locale.ToString()) : -1;

                LocalizationSettings.SelectedLocaleChanged += this.OnSelectedLocaleChanged;
            };
        }

        public event EventHandler<BindablePropertyChangedEventArgs>? propertyChanged;

        public ref Data Value => ref this.data;

        [CreateProperty]
        public List<string> Locales { get; } = new();

        [CreateProperty]
        public int SelectedLocale
        {
            get => this.selectedLocale;
            set
            {
                if (this.selectedLocale == value)
                {
                    return;
                }

                this.selectedLocale = value;
                LocalizationSettings.SelectedLocale = value != -1 ? LocalizationSettings.AvailableLocales.Locales[value] : default;
                this.Notify(nameof(this.SelectedLocale));
            }
        }

        public void Dispose()
        {
            if (LocalizationSettings.HasSettings)
            {
                LocalizationSettings.SelectedLocaleChanged -= this.OnSelectedLocaleChanged;
            }
        }

        private void Notify(string property)
        {
            this.propertyChanged?.Invoke(this, new BindablePropertyChangedEventArgs(property));
        }

        private void OnSelectedLocaleChanged(Locale obj)
        {
            this.SelectedLocale = this.Locales.IndexOf(LocalizationSettings.SelectedLocale.ToString());
        }

        public struct Data
        {
        }
    }
}
#endif
