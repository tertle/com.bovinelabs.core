namespace BovineLabs.Core.Editor.Internal
{
    using Unity.Entities.UI;
    using UnityEngine.UIElements;

    public static class PropertyInspector
    {
        public static VisualElement Make(object target)
        {
            return PropertyElement.MakeWithValue(target);
        }
    }
}
