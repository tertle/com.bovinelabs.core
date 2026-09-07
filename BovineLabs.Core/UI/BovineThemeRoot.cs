// <copyright file="BovineThemeRoot.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.UI
{
    using UnityEngine.UIElements;

    /// <summary>A declarative theme scope with ordinary VisualElement layout and picking behavior.</summary>
    [UxmlElement]
    public partial class BovineThemeRoot : VisualElement
    {
        public BovineThemeRoot()
        {
            BovineThemeUtility.Apply(this);
        }
    }
}
