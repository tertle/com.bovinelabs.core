// <copyright file="ElementUtility.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Inspectors
{
    using UnityEngine.UIElements;

    public static class ElementUtility
    {
        /// <summary> Adds appropriate styles to make a label match the default <see cref="BaseField{TValueType}" /> alignment in an inspector. </summary>
        /// <param name="label"> The label to apply to. </param>
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
