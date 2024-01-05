// <copyright file="EnabledBinding.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.UI
{
    using Unity.Properties;
    using UnityEngine.UIElements;

    [UxmlObject]
    public partial class EnabledBinding : CustomBinding, IDataSourceProvider
    {
        public EnabledBinding()
        {
            this.updateTrigger = BindingUpdateTrigger.OnSourceChanged;
        }

        public object? dataSource => null;

        [CreateProperty]
        public PropertyPath dataSourcePath { get; set; }

        [UxmlAttribute("data-source-path")]
        public string? DataSourcePathString
        {
            get => this.dataSourcePath.ToString();
            set => this.dataSourcePath = new PropertyPath(value);
        }

        protected override BindingResult Update(in BindingContext context)
        {
            var source = context.dataSource;
            var path = context.dataSourcePath;

            if (!PropertyContainer.TryGetValue(ref source, in path, out bool enabled))
            {
                return new BindingResult(BindingStatus.Failure, "Property not found");
            }

            context.targetElement.SetEnabled(enabled);
            return new BindingResult(BindingStatus.Success);
        }
    }
}
