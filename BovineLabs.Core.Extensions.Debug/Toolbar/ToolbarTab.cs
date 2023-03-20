// <copyright file="ToolbarTab.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Debug.Toolbar
{
    using System.Collections.Generic;
    using UnityEngine.UIElements;

    internal class ToolbarTab
    {
        public ToolbarTab(Button button, VisualElement parent)
        {
            this.Button = button;
            this.Parent = parent;
        }

        public Button Button { get; }

        public VisualElement Parent { get; }

        public List<(Group Group, VisualElement Parent)> Groups { get; } = new();

        public class Group
        {
            public Group(string name, VisualElement rootElement)
            {
                this.Name = name;
                this.RootElement = rootElement;
            }

            /// <summary> Gets the name of the group, shown below. </summary>
            public string Name { get; }

            /// <summary> Gets the root visual element of the tab. </summary>
            public VisualElement RootElement { get; }
        }
    }
}
