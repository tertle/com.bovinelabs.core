namespace BovineLabs.Core.Editor.Internal
{
    using UnityEngine.UIElements;

    public static class InspectorUtility
    {
        public static void AddRuntimeBar(VisualElement parent)
        {
            Unity.Entities.Editor.InspectorUtility.AddRuntimeBar(parent);
        }
    }
}
