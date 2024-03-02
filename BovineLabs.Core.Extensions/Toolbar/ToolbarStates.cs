// <copyright file="ToolbarStates.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_TOOLBAR
namespace BovineLabs.Core.Toolbar
{
    using System.Collections.Generic;
    using BovineLabs.Core.Keys;
    using BovineLabs.Core.Settings;
    using BovineLabs.Core.UI;
    using UnityEngine;
    using UnityEngine.UIElements;

    [SettingsGroup("Core")]
    public class ToolbarStates : UIStatesBase
    {
        /// <inheritdoc/>
        protected override void Init()
        {
            K<ToolbarStates>.Initialize(this.Keys);
        }

#if UNITY_EDITOR
        private void Reset()
        {
            var keys = new List<NameUI>();

            byte index = 0;
            AddCoreToolbar(keys, ref index, "entities", "EntitiesGroup");
            AddCoreToolbar(keys, ref index, "fps", "FPSGroup");
            AddCoreToolbar(keys, ref index, "localization", "LocalizationGroup");
            AddCoreToolbar(keys, ref index, "memory", "MemoryGroup");
            AddCoreToolbar(keys, ref index, "physics", "PhysicsGroup");
            AddCoreToolbar(keys, ref index, "quality", "QualityGroup");
            AddCoreToolbar(keys, ref index, "time", "TimeGroup");

            this.SetKeys(keys.ToArray());
        }

        private static void AddCoreToolbar(List<NameUI> keys, ref byte index, string key, string fileName)
        {
            const string dir = "Packages/com.bovinelabs.core/BovineLabs.Core.Extensions.Debug/ToolbarTabs/Assets/";
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(dir + fileName + ".uxml");
            if (asset == null)
            {
                Debug.LogError($"Asset {fileName} missing for toolbar. Please report");
                return;
            }

            keys.Add(new NameUI(key, index++, asset));
        }
#endif
    }
}
#endif
