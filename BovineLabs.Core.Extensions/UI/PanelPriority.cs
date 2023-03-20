// <copyright file="PanelPriority.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.UI
{
    /// <summary> Static priorities for panel ordering. </summary>
    public static class PanelPriority
    {
        /// <summary> Gets the lowest priority for background elements. </summary>
        public const int Background = -1000;

        /// <summary> Gets the default priority. </summary>
        public const int Default = 0;

        /// <summary> Gets the highest priority for popup elements. </summary>
        public const int Popup = 1000;
    }
}
