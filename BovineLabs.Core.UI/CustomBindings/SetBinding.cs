// <copyright file="SetBinding.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.UI
{
    using System;
    using Unity.Properties;
    using UnityEngine.UIElements;

    [UxmlObject]
    public partial class SetBinding : CustomBinding, IDataSourceProvider
    {
        public SetBinding() => this.updateTrigger = BindingUpdateTrigger.OnSourceChanged;

        [CreateProperty]
        public object dataSource => null!;

        /// <inheritdoc/>
        [CreateProperty]
        public PropertyPath dataSourcePath { get; set; }

        [UxmlAttribute("data-source-type")]
        [UxmlTypeReference(typeof(object))]
        public Type DataSourceString { get; set; }

        [UxmlAttribute("data-source-path")]
        public string DataSourcePathString
        {
            get => this.dataSourcePath.ToString();
            set => this.dataSourcePath = new PropertyPath(value);
        }

        /// <inheritdoc/>
        protected override void OnDataSourceChanged(in DataSourceContextChanged context)
        {
            var source = context.newContext.dataSource;
            var path = context.newContext.dataSourcePath;

            if (source == null)
            {
                return;
            }

            if (PropertyContainer.TryGetValue<object, Binding>(ref source, in path, out var ls))
            {
                context.targetElement.SetBinding(context.bindingId, ls);
            }
        }
    }
}
