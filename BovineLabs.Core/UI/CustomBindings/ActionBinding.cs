// <copyright file="ActionBinding.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_2023_3_OR_NEWER
namespace BovineLabs.Core.UI
{
    using System;
    using System.Collections.Generic;
    using Unity.Properties;
    using UnityEngine.UIElements;

    [UxmlObject]
    public partial class ActionBinding : CustomBinding, IDataSourceProvider
    {
        // Caching the delegate used for cleanup purposes.
        private readonly Dictionary<VisualElement, Action> cachedDelegates = new();

        /// <inheritdoc/>
        public object? dataSource => null;

        /// <inheritdoc/>
        [CreateProperty]
        public PropertyPath dataSourcePath { get; set; }

        [CreateProperty]
        public bool Invert { get; set; }

        [UxmlAttribute("data-source-path")]
        public string? DataSourcePathString
        {
            get => this.dataSourcePath.ToString();
            set => this.dataSourcePath = new PropertyPath(value);
        }

        [UxmlAttribute("inverted")]
        public string? InvertString
        {
            get => this.Invert ? true.ToString() : false.ToString();
            set
            {
                bool.TryParse(value, out var b);
                this.Invert = b;
            }
        }

        /// <inheritdoc/>
        protected override void OnDataSourceChanged(in DataSourceContextChanged context)
        {
            if (context.targetElement is not Button button)
            {
                return;
            }

            // Clean previous callbacks
            if (this.cachedDelegates.TryGetValue(button, out var action))
            {
                button.clicked -= action;
                this.cachedDelegates.Remove(button);
            }

            // Extract the `Action` from the hierarchy and register it.
            var source = context.newContext.dataSource;
            var path = context.newContext.dataSourcePath;

            if (source == null)
            {
                return;
            }

            if (PropertyContainer.TryGetValue<object, bool>(ref source, in path, out _))
            {
                // Bind to bool
                action = () => PropertyContainer.TrySetValue(ref source, in path, !this.Invert);
            }
            else if (PropertyContainer.TryGetValue(ref source, in path, out action))
            {
                // Bind to an action
            }
            else
            {
                // Unsupported type
                return;
            }

            // Actually subscribe
            button.clicked += action;
            this.cachedDelegates.Add(button, action!);
        }
    }
}
#endif
