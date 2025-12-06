// <copyright file="ViewModelToolbar.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_6000_3_OR_NEWER
namespace BovineLabs.Core.Editor.UI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using JetBrains.Annotations;
    using UnityEditor;
    using UnityEditor.Toolbars;
    using UnityEngine;
    using UnityEngine.UIElements;
    using Object = UnityEngine.Object;

    public static class ViewModelToolbar
    {
        private const string StartupPath = "BovineLabs/View Model";

        private static HashSet<object> viewModels = new();

        static ViewModelToolbar()
        {
            EditorApplication.playModeStateChanged += change =>
            {
                switch (change)
                {
                    case PlayModeStateChange.EnteredPlayMode:
                    case PlayModeStateChange.EnteredEditMode:
                        MainToolbar.Refresh(StartupPath);
                        break;
                }
            };
        }

        [UsedImplicitly]
        [MainToolbarElement(StartupPath, defaultDockPosition = MainToolbarDockPosition.Right)]
        public static MainToolbarElement Inspector()
        {
            var icon = EditorGUIUtility.IconContent("d_GUISkin On Icon").image as Texture2D;
            var content = new MainToolbarContent(icon);
            return new MainToolbarDropdown(content, EditorStartupDropDown) { enabled = EditorApplication.isPlayingOrWillChangePlaymode };
        }

        private static void EditorStartupDropDown(Rect worldBound)
        {
            viewModels.Clear();

            var documents = Object.FindObjectsByType<UIDocument>(FindObjectsSortMode.None).Where(d => d.rootVisualElement != null).ToArray();

            foreach (var d in documents)
            {
                foreach (var ve in d.rootVisualElement.Query<VisualElement>().Build())
                {
                    if (ve.dataSource != null)
                    {
                        viewModels.Add(ve.dataSource);
                    }
                }
            }

            if (viewModels.Count == 0)
            {
                return;
            }

            var menu = new GenericMenu();

            var groups = viewModels.GroupBy(vm => vm.GetType().Name.Contains("Toolbar")).OrderBy(g => g.Key).ToArray();

            for (var index = 0; index < groups.Length; index++)
            {
                var group = groups[index];

                if (index > 0)
                {
                    menu.AddSeparator(string.Empty);
                }

                foreach (var vm in group.OrderBy(vm => vm.GetType().Name))
                {
                    var type = vm.GetType();
                    var name = GetFriendlyName(type);
                    menu.AddItem(EditorGUIUtility.TrTextContent(name, type.Namespace), false, static data =>
                    {
                        Selection.activeObject = ObjectSelectionProxy.CreateInstance(data);
                    }, vm);
                }
            }

            menu.DropDown(worldBound);
        }

        private static string GetFriendlyName(Type type)
        {
            if (type.IsGenericType)
            {
                var typeName = type.Name;
                var backtickIndex = typeName.IndexOf('`');
                if (backtickIndex > 0)
                {
                    typeName = typeName[..backtickIndex];
                }

                var genericArgs = string.Join(", ", type.GetGenericArguments().Select(GetFriendlyName));

                return $"{typeName}<{genericArgs}>";
            }

            return type.Name switch
            {
                "Int32" => "int",
                "String" => "string",
                "Boolean" => "bool",
                "Object" => "object",
                "Void" => "void",
                "Char" => "char",
                "Decimal" => "decimal",
                "Double" => "double",
                "Single" => "float",
                "Byte" => "byte",
                "SByte" => "sbyte",
                "Int16" => "short",
                "Int64" => "long",
                "UInt16" => "ushort",
                "UInt32" => "uint",
                "UInt64" => "ulong",
                _ => type.Name,
            };
        }
    }
}
#endif