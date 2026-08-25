// <copyright file="ComponentAssetEditor.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Component
{
    using UnityEditor;

    [CustomEditor(typeof(ComponentAsset), true, isFallback = true)]
    public class ComponentAssetEditor : TypeAssetEditor
    {
        protected override string SearchQuery => this.target switch
        {
            ComponentTagAsset => "componentdata=true zerosized=true editor=false",
            ComponentEnableableAsset => "component=true enableable=true editor=false",
            _ => "component=true editor=false",
        };
    }
}
