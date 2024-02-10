// <copyright file="UIAssetManagement.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_UI
namespace BovineLabs.Core.UI
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UIElements;

    public interface IUIAssetManagement
    {
        object? GetPanel(int id);
    }

    public abstract class UIAssetManagement : MonoBehaviour, IUIAssetManagement
    {
        private readonly Dictionary<int, VisualElement> loadedPanels = new();
        private readonly Dictionary<int, VisualTreeAsset> allPanels = new();

        [SerializeField]
        private UIStatesBase? panels;

        public object GetPanel(int id)
        {
            return this.loadedPanels[id];
        }

        protected void LoadAllPanels()
        {
            if (this.panels == null)
            {
                Debug.LogError($"Panels not setup for {this.GetType()}");
                return;
            }

            foreach (var p in this.panels.Data)
            {
                if (p.Asset == null)
                {
                    Debug.LogWarning($"Asset for {p.Value} is not set");
                    continue;
                }

                this.allPanels.Add(p.Value, p.Asset);
            }
        }

        protected bool TryLoadPanel(int id, int assetKey, out VisualElement element)
        {
            if (this.loadedPanels.TryGetValue(id, out element))
            {
                Unity.Debug.LogError($"Panel with id {id} already loaded");
                return false;
            }

            if (!this.allPanels.TryGetValue(assetKey, out var assets))
            {
                Debug.LogError($"Panel {assetKey} not setup");
                element = null!;
                return false;
            }

            var visualElement = assets.CloneTree();
            this.loadedPanels.Add(id, visualElement);

            element = visualElement;
            return true;
        }

        protected bool TryUnloadPanel(int id, out VisualElement panel)
        {
            return this.loadedPanels.Remove(id, out panel);
        }
    }
}
#endif
