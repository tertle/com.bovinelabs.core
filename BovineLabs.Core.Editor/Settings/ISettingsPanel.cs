// <copyright file="ISettingsPanel.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Settings
{
    using UnityEngine.UIElements;

    /// <summary> The panel interface for <see cref="SettingsBaseWindow{T}" />. </summary>
    public interface ISettingsPanel
    {
        /// <summary> Gets the display name for the panel. </summary>
        string DisplayName { get; }

        string GroupName { get; }

        bool IsEmpty { get; }

        /// <summary> Called when panel is activated. Use this to draw UI toolkit. </summary>
        /// <param name="searchContext"> The search context. </param>
        /// <param name="rootElement"> The root element that can be used to draw UI toolkit. </param>
        void OnActivate(string searchContext, VisualElement rootElement);

        /// <summary> Called when panel is deactivated. </summary>
        void OnDeactivate();

        /// <summary> Checks if the panel matches a search context for filtering. </summary>
        /// <param name="searchContext"> The context to match. </param>
        /// <param name="allowEmpty"> Are empty panels allowed to match. </param>
        /// <returns> True if this panel matches the context. </returns>
        bool MatchesFilter(string searchContext, bool allowEmpty);
    }
}
