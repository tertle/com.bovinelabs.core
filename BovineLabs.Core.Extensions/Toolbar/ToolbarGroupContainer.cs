// <copyright file="ToolbarGroupContainer.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_6000_0_OR_NEWER
namespace BovineLabs.Core.Toolbar
{
    using Unity.Properties;
    using UnityEngine.UIElements;

    [UxmlElement]
    public partial class ToolbarGroupContainer : VisualElement
    {
        private const string ToolbarGroupNameClass = "group-name";
        private const string ToolbarGroupClass = "bl-toolbar-group";

        private readonly VisualElement content;
        private readonly Label groupLabel;

        public ToolbarGroupContainer()
            : this(string.Empty)
        {
        }

        public ToolbarGroupContainer(string label)
        {
            this.AddToClassList(ToolbarGroupClass);

            this.content = new VisualElement();
            this.hierarchy.Add(this.content);

            this.groupLabel = new Label(label);
            this.groupLabel.AddToClassList(ToolbarGroupNameClass);
            this.hierarchy.Add(this.groupLabel);
        }

        public override VisualElement contentContainer => this.content;

        [UxmlAttribute]
        [CreateProperty]
        public string label
        {
            get => this.groupLabel.text;
            set => this.groupLabel.text = value;
        }
    }
}
#endif
