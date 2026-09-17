namespace BovineLabs.Core.Editor.Inspectors
{
    using UnityEngine.UIElements;

    public static class ElementUtility
    {
        public static void AddLabelStyles(Label label)
        {
            label.AddToClassList(BaseField<string>.ussClassName);
            label.AddToClassList(BaseField<string>.labelUssClassName);
            label.AddToClassList(BaseField<string>.ussClassName + "__inspector-field");
            label.style.minHeight = new StyleLength(19); // bit gross but matches the element
        }

        public static void SetVisible(VisualElement element, bool visible)
        {
            element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
