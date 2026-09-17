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
