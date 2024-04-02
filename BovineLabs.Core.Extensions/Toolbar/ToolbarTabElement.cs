// <copyright file="ToolbarTabElement.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_2023_3_OR_NEWER
namespace BovineLabs.Core.Toolbar
{
    using UnityEngine.UIElements;

    [UxmlElement]
    public partial class ToolbarTabElement : VisualElement
    {
        private const string ContentsClass = "bl-toolbar-contents";

        public ToolbarTabElement()
        {
            this.AddToClassList(ContentsClass);
        }
    }
}
#endif
