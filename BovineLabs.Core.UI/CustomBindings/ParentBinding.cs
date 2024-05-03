// <copyright file="ParentBinding.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.UI
{
    using System.Collections.Generic;
    using Unity.Properties;
    using UnityEngine.UIElements;

    [UxmlObject]
    public partial class ParentBinding : CustomBinding, IDataSourceProvider
    {
        private readonly Dictionary<VisualElement, List<VisualElement>> cachedParents = new();

        public ParentBinding()
        {
            this.updateTrigger = BindingUpdateTrigger.OnSourceChanged;
        }

        public object dataSource => null;

        [CreateProperty]
        public PropertyPath dataSourcePath { get; set; }

        [UxmlAttribute("data-source-path")]
        public string DataSourcePathString
        {
            get => this.dataSourcePath.ToString();
            set => this.dataSourcePath = new PropertyPath(value);
        }

        protected override BindingResult Update(in BindingContext context)
        {
            var source = context.dataSource;
            var path = context.dataSourcePath;

            if (!PropertyContainer.TryGetValue(ref source, in path, out bool allowChildren))
            {
                return new BindingResult(BindingStatus.Failure, "Property not found");
            }

            var target = context.targetElement;

            if (allowChildren)
            {
                if (this.cachedParents.TryGetValue(target, out var children))
                {
                    foreach (var c in children)
                    {
                        target.Add(c);
                    }

                    children.Clear();
                }
            }
            else
            {
                if (target.childCount > 0)
                {
                    if (!this.cachedParents.TryGetValue(target, out var children))
                    {
                        this.cachedParents[target] = children = new List<VisualElement>();
                    }

                    var count = target.childCount;
                    for (var i = 0; i < count; i++)
                    {
                        var child = target[0];
                        children.Add(child);
                        target.RemoveAt(0);
                    }
                }
            }

            return new BindingResult(BindingStatus.Success);
        }
    }
}
