// <copyright file="ProjectView.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Utility
{
    using System;
    using System.IO;
    using System.Reflection;
    using Unity.Scripting.LifecycleManagement;
    using UnityEditor;
    using Object = UnityEngine.Object;

    public static class ProjectView
    {
        public static class Internal
        {
            [NoAutoStaticsCleanup]
            private static readonly Func<string> GetActiveFolderPathFunc;

            [NoAutoStaticsCleanup]
            private static readonly Func<object> GetProjectBrowserIfExistsFunc;

            [NoAutoStaticsCleanup]
            private static readonly MethodInfo ShowFolderContentsMethod;

            [NoAutoStaticsCleanup]
            private static readonly MethodInfo EndPingMethod;

            [NoAutoStaticsCleanup]
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

            [NoAutoStaticsCleanup]
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
                    ShowFolderContentsMethod.Invoke(projectBrowser, new object[] { folderAsset.GetEntityId(), true });
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
