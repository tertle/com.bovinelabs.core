// <copyright file="UIElementsExtensionsEditor.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.UI
{
    using System.Linq;
    using System.Reflection;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;

    public static class UIElementsExtensionsEditor
    {
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
    }
}
