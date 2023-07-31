// <copyright file="UIElementsExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.UI
{
    using System.Linq;
    using System.Reflection;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;

    /// <summary> Extensions and helpers for UIToolkit. </summary>
    public static class UIElementsExtensions
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

        public static void RegisterDisabledContextualMenuManipulator(PropertyField target, ContextualMenuManipulator manipulator)
        {
            var invokePolicyType = typeof(CallbackEventHandler).Assembly.GetType("UnityEngine.UIElements.InvokePolicy");
            var registerCallback = typeof(CallbackEventHandler).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
                .Where(x => x.Name == "RegisterCallback" && x.IsGenericMethodDefinition)
                .Single(x =>
                {
                    var p = x.GetParameters();
                    if (p.Length != 3)
                    {
                        return false;
                    }

                    return p[0].ParameterType == typeof(EventCallback<>).MakeGenericType(x.GetGenericArguments()[0]) &&
                           p[1].ParameterType == invokePolicyType &&
                           p[2].ParameterType == typeof(TrickleDown);
                });

            var invokePolicy = 1; // Allow when disabled

            typeof(Manipulator).GetField("m_Target", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(manipulator, target);

            if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer)
            {
                var onMouseDownEventOSX = typeof(ContextualMenuManipulator)
                    .GetMethod("OnMouseDownEventOSX", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .CreateDelegate(typeof(EventCallback<MouseDownEvent>), manipulator);

                var onMouseUpEventOSX = typeof(ContextualMenuManipulator)
                    .GetMethod("OnMouseUpEventOSX", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .CreateDelegate(typeof(EventCallback<MouseUpEvent>), manipulator);

                registerCallback.MakeGenericMethod(typeof(MouseDownEvent))
                    .Invoke(target, new object[] { onMouseDownEventOSX, invokePolicy, TrickleDown.NoTrickleDown });

                registerCallback.MakeGenericMethod(typeof(MouseUpEvent))
                    .Invoke(target, new object[] { onMouseUpEventOSX, invokePolicy, TrickleDown.NoTrickleDown });
            }
            else
            {
                var onMouseUpDownEvent = typeof(ContextualMenuManipulator)
                    .GetMethod("OnMouseUpDownEvent", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .CreateDelegate(typeof(EventCallback<MouseUpEvent>), manipulator);

                registerCallback.MakeGenericMethod(typeof(MouseUpEvent))
                    .Invoke(target, new object[] { onMouseUpDownEvent, invokePolicy, TrickleDown.NoTrickleDown });
            }

            var onKeyUpEvent = typeof(ContextualMenuManipulator)
                .GetMethod("OnKeyUpEvent", BindingFlags.Instance | BindingFlags.NonPublic)!
                .CreateDelegate(typeof(EventCallback<KeyUpEvent>), manipulator);

            var onContextualMenuEvent = typeof(ContextualMenuManipulator)
                .GetMethod("OnContextualMenuEvent", BindingFlags.Instance | BindingFlags.NonPublic)!
                .CreateDelegate(typeof(EventCallback<ContextualMenuPopulateEvent>), manipulator);

            registerCallback.MakeGenericMethod(typeof(KeyUpEvent))
                .Invoke(target, new object[] { onKeyUpEvent, invokePolicy, TrickleDown.NoTrickleDown });

            registerCallback.MakeGenericMethod(typeof(ContextualMenuPopulateEvent))
                .Invoke(target, new object[] { onContextualMenuEvent, invokePolicy, TrickleDown.NoTrickleDown });
        }

        private static void StopPropagation(EventBase e)
        {
            e.StopPropagation();
        }
    }
}
