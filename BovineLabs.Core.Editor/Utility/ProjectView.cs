// <copyright file="ProjectView.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Utility
{
    using System;
    using System.IO;
    using System.Reflection;
    using UnityEditor;
    using Object = UnityEngine.Object;

    public static class ProjectView
    {
        public static class Internal
        {
            private static readonly Func<string> GetActiveFolderPathFunc;
            private static readonly Func<object> GetProjectBrowserIfExistsFunc;

            private static readonly MethodInfo ShowFolderContentsMethod;
            private static readonly MethodInfo EndPingMethod;

            private static readonly FieldInfo ViewMode;

            static Internal()
            {
                var projectWindowUtilType = typeof(ProjectWindowUtil);
                var getActiveFolderPathMethod = projectWindowUtilType.GetMethod("GetActiveFolderPath", BindingFlags.Static | BindingFlags.NonPublic);
                GetActiveFolderPathFunc = (Func<string>)Delegate.CreateDelegate(typeof(Func<string>), getActiveFolderPathMethod!);

                var getProjectBrowserIfExistsMethod = projectWindowUtilType.GetMethod("GetProjectBrowserIfExists", BindingFlags.Static | BindingFlags.NonPublic);
                GetProjectBrowserIfExistsFunc = (Func<object>)Delegate.CreateDelegate(typeof(Func<object>), getProjectBrowserIfExistsMethod!);

                ViewMode = getProjectBrowserIfExistsMethod!.ReturnType.GetField("m_ViewMode", BindingFlags.Instance | BindingFlags.NonPublic)!;

                ProjectBrowserType = typeof(Editor).Assembly.GetType("UnityEditor.ProjectBrowser");
                ShowFolderContentsMethod = ProjectBrowserType.GetMethod("ShowFolderContents", BindingFlags.Instance | BindingFlags.NonPublic)!;
                EndPingMethod = ProjectBrowserType.GetMethod("EndPing", BindingFlags.Instance | BindingFlags.NonPublic)!;
            }

            public static Type ProjectBrowserType { get; }

            public static string GetDirectory()
            {
                var isTwoColumnView = IsTwoColumnView();

                if (!isTwoColumnView && Selection.objects.Length == 1)
                {
                    var assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
                    return AssetDatabase.IsValidFolder(assetPath) ? assetPath + "/" : Path.GetDirectoryName(assetPath)!.Replace("\\", "/");
                }

                return GetActiveFolderPathFunc();
            }

            public static void ShowFolderContents(object projectBrowser, string path)
            {
                var folderAsset = AssetDatabase.LoadAssetAtPath<Object>(path);

                if (IsTwoColumnView())
                {
#if UNITY_6000_3_OR_NEWER
                    ShowFolderContentsMethod.Invoke(projectBrowser, new object[] { folderAsset.GetEntityId(), true });
#else
                    ShowFolderContentsMethod.Invoke(projectBrowser, new object[] { folderAsset.GetInstanceID(), true});
#endif
                }
            }

            public static bool IsTwoColumnView()
            {
                var browser = GetProjectBrowserIfExistsFunc();
                if (browser == null)
                {
                    return true;
                }

                var mode = ViewMode.GetValue(browser)!;
                return !Convert.ChangeType(mode, Enum.GetUnderlyingType(mode.GetType())).Equals(0);
            }

            public static void EndPing(object projectBrowser)
            {
                EndPingMethod.Invoke(projectBrowser, null);
            }
        }
    }
}
