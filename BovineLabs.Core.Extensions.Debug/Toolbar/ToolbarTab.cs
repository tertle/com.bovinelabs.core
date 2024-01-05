// <copyright file="ToolbarTab.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_TOOLBAR
namespace BovineLabs.Core.Toolbar
{
    using System.Collections.Generic;
    using UnityEngine.UIElements;

    internal class ToolbarTab
    {
        public ToolbarTab(string name, Button button, VisualElement parent)
        {
            this.Name = name;
            this.Button = button;
            this.Parent = parent;
        }

        public string Name { get; }

        public Button Button { get; }

        public VisualElement Parent { get; }

        public List<Group> Groups { get; } = new();

        public class Group
        {
            public Group(int id, string name, ToolbarGroupContainer container, ToolbarTab tab)
            {
                this.Name = name;
                this.ID = id;
                this.Container = container;
                this.Tab = tab;
            }


            /// <summary> Gets the name of the group, shown below. </summary>
            public string Name { get; }

            public int ID { get; }

            /// <summary> Gets the root visual element of the tab. </summary>
            public ToolbarGroupContainer Container { get; }

            public ToolbarTab Tab { get; }
        }
    }
}
#endif
