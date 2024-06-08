// <copyright file="UIAssetManagement.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_UI
namespace BovineLabs.Core.UI
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UIElements;

    public interface IUIAssetManagement
    {
        object GetPanel(int id);
    }

    public abstract class UIAssetManagement : MonoBehaviour, IUIAssetManagement
    {
        private readonly Dictionary<int, (VisualElement Element, IBindingObject Binding)> loadedPanels = new();
        private readonly Dictionary<int, VisualTreeAsset> allPanels = new();

        [SerializeField]
        private UIStatesBase panels;

        public object GetPanel(int id)
        {
            return this.loadedPanels[id].Element;
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

        protected bool TryLoadPanel<T>(int id, int assetKey, out (VisualElement Element, T Binding) panel)
            where T : class, IBindingObject, new()
        {
            if (this.loadedPanels.TryGetValue(id, out var panel2))
            {
                Unity.Debug.LogError($"Panel with id {id} already loaded");
                if (panel2.Binding is not T existingBinding)
                {
                    throw new InvalidOperationException("Trying to load a binding of a different type");
                }

                panel = (panel2.Element, existingBinding);

                return false;
            }

            if (!this.allPanels.TryGetValue(assetKey, out var assets))
            {
                Debug.LogError($"Panel {assetKey} not setup");
                panel = (null!, null!);
                return false;
            }

            var visualElement = assets.CloneTree();

            var binding = new T();

            this.loadedPanels.Add(id, (visualElement, binding));

            panel = (visualElement, binding);
            return true;
        }

        protected bool TryUnloadPanel(int id, out (VisualElement Element, IBindingObject Binding) panel)
        {
            return this.loadedPanels.Remove(id, out panel);
        }
    }
}
#endif
