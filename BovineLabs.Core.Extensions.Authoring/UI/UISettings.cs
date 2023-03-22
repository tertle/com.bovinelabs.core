// <copyright file="UISettings.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_UI
namespace BovineLabs.Core.Authoring.UI
{
    using System;
    using BovineLabs.Core.Authoring.Settings;
    using BovineLabs.Core.Keys;
    using BovineLabs.Core.UI;
    using Unity.Entities;
    using UnityEngine;
#if UNITY_LOCALIZATION
    using UnityEngine.Localization;
#endif
    using UnityEngine.UIElements;

    public class UISettings : Settings
    {
        [SerializeField]
        private KeyWindow[] windows = Array.Empty<KeyWindow>();

#if UNITY_LOCALIZATION
        [SerializeField]
        private LocalizedStringTable? stringLocalization;
#endif

        /// <inheritdoc />
        public override void Bake(IBaker baker)
        {
            var assets = new VisualTreeAsset[256];

            foreach (var window in this.windows)
            {
                if (window.Asset == null)
                {
                    Debug.LogWarning($"No asset assigned for {window.Key}");
                    continue;
                }

                if (assets[window.Key] != null)
                {
                    Debug.LogError($"More than 1 ui window assigned to {window.Key}");
                    continue;
                }

                assets[window.Key] = window.Asset;
            }

            baker.AddComponentObject(new UIAssets
            {
                Assets = assets,
#if UNITY_LOCALIZATION
                StringLocalization = this.stringLocalization?.TableReference ?? default,
#endif
            });
            baker.AddComponent<UIState>();
            baker.AddComponent<UIStatePrevious>();
            baker.AddBuffer<UIStateBack>();
            baker.AddBuffer<UIStateForward>();
        }

        [Serializable]
        private struct KeyWindow
        {
            [K("UIStates")]
            public byte Key;

            public VisualTreeAsset Asset;
        }
    }
}
#endif
