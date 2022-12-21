// <copyright file="UIToolkitExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.UI
{
    using UnityEngine.UIElements;

    /// <summary> Extensions and helpers for UIToolkit. </summary>
    public static class UIToolkitExtensions
    {
        /// <summary> Blocks keyboard and mouse events passing through an element. Good for the background. </summary>
        /// <param name="element"> The element that blocks. </param>
        public static void BlockInputEvents(this VisualElement element)
        {
            element.RegisterCallback<MouseDownEvent>(StopPropagation);
            element.RegisterCallback<MouseUpEvent>(StopPropagation);
            element.RegisterCallback<KeyDownEvent>(StopPropagation);
            element.RegisterCallback<KeyUpEvent>(StopPropagation);
        }

        /// <summary> Gets the root element of an element. </summary>
        /// <remarks>
        /// Has a special case for the class "unity-ui-document__root".
        /// </remarks>
        /// <param name="element"> The child. </param>
        /// <returns> The root element. </returns>
        public static VisualElement Root(this VisualElement element)
        {
            var p = element;

            if (p.parent == null)
            {
                return p;
            }

            do
            {
                if (p.parent.ClassListContains("unity-ui-document__root"))
                {
                    return p.parent;
                }

                p = p.parent;
            }
            while (p.parent != null);

            return p;
        }

        /// <summary> Checks if an element is in a parent in the hierarchy. </summary>
        /// <param name="ext"> The child element. </param>
        /// <param name="visualElement"> The parent element to check. </param>
        /// <returns> True if found in hierarchy, otherwise false. </returns>
        public static bool InHierarchy(this VisualElement ext, VisualElement visualElement)
        {
            var p = ext;

            if (ext == visualElement)
            {
                return true;
            }

            if (p.parent == null)
            {
                return false;
            }

            do
            {
                if (p.parent == visualElement)
                {
                    return true;
                }

                p = p.parent;
            }
            while (p.parent != null);

            return false;
        }

        private static void StopPropagation(EventBase e)
        {
            e.StopPropagation();
        }
    }
}
