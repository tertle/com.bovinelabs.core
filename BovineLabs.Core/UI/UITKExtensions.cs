// <copyright file="UITKExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.UI
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using UnityEngine.UIElements;

    public static class UITKExtensions
    {
        private static readonly Delegate BaseVisualElementPanel_TopElementUnderPointer;
        private static readonly Func<VisualElement, object> VisualElement_GetElementPanel;
        private static readonly Func<VisualElement, VisualElement> VisualElement_GetRootVisualContainer;
        private static readonly Action<GenericDropdownMenu, string, bool> GenericDropdownMenu_UpdateItem;
        // private static readonly Action<GenericDropdownMenu, bool> GenericDropdownMenu_SetAutoClose; // new
        private static readonly Action<GenericDropdownMenu, bool> GenericDropdownMenu_IsSingleSelectionDropdown; // old
        private static readonly Func<GenericDropdownMenu, VisualElement> GenericDropdownMenu_GetMenuContainer;
        // private static readonly Func<GenericDropdownMenu, ListView> GenericDropdownMenu_GetInnerContainer;
        // private static readonly Func<GenericDropdownMenu, VisualElement> GenericDropdownMenu_GetOuterContainer;
        private static readonly NotifyPropertyChangedDelegate CallbackEventHandler_NotifyPropertyChanged;

        static UITKExtensions()
        {
            BaseVisualElementPanel_TopElementUnderPointer = GetTopElementUnderPointer();
            VisualElement_GetElementPanel = GetElementPanel();
            VisualElement_GetRootVisualContainer = VisualElementGetRootVisualContainer();
            GenericDropdownMenu_UpdateItem = GetUpdateItem();
            // GenericDropdownMenu_SetAutoClose = SetAutoClose();
            GenericDropdownMenu_IsSingleSelectionDropdown = SetIsSingleSelectionDropdown();
            GenericDropdownMenu_GetMenuContainer = GetMenuContainer();
            // GenericDropdownMenu_GetInnerContainer = GetGenericDropdownMenuInnerContainer();
            // GenericDropdownMenu_GetOuterContainer = GetGenericDropdownMenuOuterContainer();
            CallbackEventHandler_NotifyPropertyChanged = GetNotifyPropertyChanged();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VisualElement GetTopElementUnderPointer(this VisualElement visualElement, int pointerId)
        {
            var elementPanel = VisualElement_GetElementPanel.Invoke(visualElement);
            return (VisualElement)BaseVisualElementPanel_TopElementUnderPointer.DynamicInvoke(elementPanel, pointerId);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VisualElement GetRootVisualContainer(this VisualElement visualElement)
        {
            return VisualElement_GetRootVisualContainer.Invoke(visualElement);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void UpdateItem(this GenericDropdownMenu menu, string itemName, bool isChecked)
        {
            GenericDropdownMenu_UpdateItem(menu, itemName, isChecked);
        }

        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // public static void SetAutoClose(this GenericDropdownMenu menu, bool autoClose)
        // {
        //     GenericDropdownMenu_SetAutoClose(menu, autoClose);
        // }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetIsSingleSelectionDropdown(this GenericDropdownMenu menu, bool autoClose)
        {
            GenericDropdownMenu_IsSingleSelectionDropdown(menu, autoClose);
        }

        public static List<string>? ParseStringListAttribute(string itemList)
        {
            if (string.IsNullOrEmpty(itemList?.Trim()))
            {
                return null;
            }

            string[] strArray = itemList.Split(',');
            if (strArray.Length == 0)
            {
                return null;
            }

            List<string> stringListAttribute = new List<string>();
            foreach (string str in strArray)
            {
                stringListAttribute.Add(str.Trim());
            }

            return stringListAttribute;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VisualElement GetMenuContainer(this GenericDropdownMenu menu)
        {
            return GenericDropdownMenu_GetMenuContainer(menu);
        }

        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // public static ListView GetInnerContainer(this GenericDropdownMenu menu)
        // {
        //     return GenericDropdownMenu_GetInnerContainer(menu);
        // }

        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // public static VisualElement GetOuterContainer(this GenericDropdownMenu menu)
        // {
        //     return GenericDropdownMenu_GetOuterContainer(menu);
        // }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void NotifyPropertyChanged(this CallbackEventHandler callbackEventHandler, in BindingId property)
        {
            CallbackEventHandler_NotifyPropertyChanged.Invoke(callbackEventHandler, property);
        }

        private static Delegate GetTopElementUnderPointer()
        {
            var asm = typeof(VisualElement).Assembly;
            var type = asm.GetType("UnityEngine.UIElements.BaseVisualElementPanel");

            var methodInfo = type.GetMethod("GetTopElementUnderPointer", BindingFlags.Instance | BindingFlags.NonPublic);

            if (methodInfo == null)
            {
                throw new Exception("GetTopElementUnderPointer signature changed.");
            }

            var func = typeof(Func<,,>).MakeGenericType(type, typeof(int), typeof(VisualElement));
            return Delegate.CreateDelegate(func, null, methodInfo);
        }

        private static Func<VisualElement, object> GetElementPanel()
        {
            var propertyInfo = typeof(VisualElement).GetProperty("elementPanel", BindingFlags.Instance | BindingFlags.NonPublic);

            if (propertyInfo == null)
            {
                throw new Exception("elementPanel signature changed.");
            }

            return (Func<VisualElement, object>)propertyInfo.GetGetMethod(true).CreateDelegate(typeof(Func<VisualElement, object>), null);
        }

        private static Func<VisualElement, VisualElement> VisualElementGetRootVisualContainer()
        {
            var methodInfo = typeof(VisualElement).GetMethod("GetRootVisualContainer", BindingFlags.Instance | BindingFlags.NonPublic);

            if (methodInfo == null)
            {
                throw new Exception("elementPanel signature changed.");
            }

            return (Func<VisualElement, VisualElement>)methodInfo.CreateDelegate(typeof(Func<VisualElement, VisualElement>), null);
        }

        private static Action<GenericDropdownMenu, string, bool> GetUpdateItem()
        {
            var methodInfo = typeof(GenericDropdownMenu).GetMethod("UpdateItem", BindingFlags.Instance | BindingFlags.NonPublic);

            if (methodInfo == null)
            {
                throw new Exception("GetUpdateItem signature changed.");
            }

            return (Action<GenericDropdownMenu, string, bool>)methodInfo.CreateDelegate(typeof(Action<GenericDropdownMenu, string, bool>), null);
        }

        // private static Action<GenericDropdownMenu, bool> SetAutoClose()
        // {
        //     var propertyInfo = typeof(GenericDropdownMenu).GetProperty("autoClose", BindingFlags.Instance | BindingFlags.NonPublic);
        //
        //     if (propertyInfo == null)
        //     {
        //         throw new Exception("elementPanel signature changed.");
        //     }
        //
        //     return (Action<GenericDropdownMenu, bool>)propertyInfo.GetSetMethod(true).CreateDelegate(typeof(Action<GenericDropdownMenu, bool>), null);
        // }

        private static Action<GenericDropdownMenu, bool> SetIsSingleSelectionDropdown()
        {
            var propertyInfo = typeof(GenericDropdownMenu).GetProperty("isSingleSelectionDropdown", BindingFlags.Instance | BindingFlags.NonPublic);

            if (propertyInfo == null)
            {
                throw new Exception("elementPanel signature changed.");
            }

            return (Action<GenericDropdownMenu, bool>)propertyInfo.GetSetMethod(true).CreateDelegate(typeof(Action<GenericDropdownMenu, bool>), null);
        }

        private static Func<GenericDropdownMenu, VisualElement> GetMenuContainer()
        {
            var propertyInfo = typeof(GenericDropdownMenu).GetProperty("menuContainer", BindingFlags.Instance | BindingFlags.NonPublic);

            if (propertyInfo == null)
            {
                throw new Exception("menuContainer signature changed.");
            }

            return (Func<GenericDropdownMenu, VisualElement>)propertyInfo.GetGetMethod(true).CreateDelegate(typeof(Func<GenericDropdownMenu, VisualElement>), null);
        }

        // private static Func<GenericDropdownMenu, ListView> GetGenericDropdownMenuInnerContainer()
        // {
        //     var propertyInfo = typeof(GenericDropdownMenu).GetProperty("innerContainer", BindingFlags.Instance | BindingFlags.NonPublic);
        //
        //     if (propertyInfo == null)
        //     {
        //         throw new Exception("innerContainer signature changed.");
        //     }
        //
        //     return (Func<GenericDropdownMenu, ListView>)propertyInfo.GetGetMethod(true).CreateDelegate(typeof(Func<GenericDropdownMenu, ListView>), null);
        // }
        //
        // private static Func<GenericDropdownMenu, VisualElement> GetGenericDropdownMenuOuterContainer()
        // {
        //     var propertyInfo = typeof(GenericDropdownMenu).GetProperty("outerContainer", BindingFlags.Instance | BindingFlags.NonPublic);
        //
        //     if (propertyInfo == null)
        //     {
        //         throw new Exception("outerContainer signature changed.");
        //     }
        //
        //     return (Func<GenericDropdownMenu, VisualElement>)propertyInfo.GetGetMethod(true).CreateDelegate(typeof(Func<GenericDropdownMenu, VisualElement>), null);
        // }

        private static NotifyPropertyChangedDelegate GetNotifyPropertyChanged()
        {
            var methodInfo = typeof(CallbackEventHandler).GetMethod("NotifyPropertyChanged", BindingFlags.Instance | BindingFlags.NonPublic);

            if (methodInfo == null)
            {
                throw new Exception("elementPanel signature changed.");
            }

            return (NotifyPropertyChangedDelegate)methodInfo.CreateDelegate(typeof(NotifyPropertyChangedDelegate), null);
        }

        private delegate void NotifyPropertyChangedDelegate(CallbackEventHandler handler, in BindingId bindingId);
    }
}
