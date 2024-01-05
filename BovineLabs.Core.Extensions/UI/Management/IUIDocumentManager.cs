// <copyright file="IUIDocumentManager.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_UI
namespace BovineLabs.Core.UI
{
    using System;
    using UnityEngine.UIElements;

    public interface IUIDocumentManager
    {
#if UNITY_EDITOR
        /// <summary>
        /// This is used to add support for UI Toolkit Live Reload abilities for making changes at runtime in the editor.
        /// This should clean up the old element via RemovePanel and then reload your element and again pass it to AddPanel.
        /// </summary>
        event Action EditorRebuild;
#endif

        VisualElement Root { get; }

        void AddRoot(VisualElement visualElement, int priority = 0);

        void RemoveRoot(VisualElement visualElement);

        /// <summary> Adds a panel to the UI. </summary>
        /// <param name="key"> The panel to add. </param>
        /// <param name="bindingObject"> The object to bind to the panel. </param>
        /// <param name="priority"> The draw priority. </param>
        void AddPanel(int key, IBindingObject bindingObject, int priority = 0);

        /// <summary> Removes a panel from the UI. </summary>
        /// <param name="key"> The panel to remove. </param>
        void RemovePanel(int key);
    }
}
#endif
