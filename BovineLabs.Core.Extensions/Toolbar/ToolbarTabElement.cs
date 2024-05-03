// <copyright file="ToolbarTabElement.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_6000_0_OR_NEWER
namespace BovineLabs.Core.Toolbar
{
    using UnityEngine.UIElements;

    [UxmlElement]
    public partial class ToolbarTabElement : ScrollView
    {
        private const string ContentsClass = "bl-toolbar-contents";

        public ToolbarTabElement()
        {
            this.AddToClassList(ContentsClass);
            this.mode = ScrollViewMode.Horizontal;
            this.verticalScrollerVisibility = ScrollerVisibility.Hidden;

            this.horizontalScroller.RemoveFromHierarchy();
        }

        public void AddToTab(VisualElement tab)
        {
            tab.Add(this);
            tab.Add(this.horizontalScroller);
        }

        public void RemoveFromTab()
        {
            this.RemoveFromHierarchy();
            this.horizontalScroller.RemoveFromHierarchy();
        }
    }
}
#endif
