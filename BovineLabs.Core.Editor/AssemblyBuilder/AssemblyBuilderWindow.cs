// <copyright file="AssemblyBuilderWindow.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.AssemblyBuilder
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using BovineLabs.Core.Editor.UI;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UIElements;

    /// <summary> An editor window that allows easy creation of new assembly definitions. </summary>
    public class AssemblyBuilderWindow : EditorWindow
    {
        private const string AssemblyInfoTemplate =
            "// <copyright file=\"AssemblyInfo.cs\" company=\"{0}\">\n// Copyright (c) {0}. All rights reserved.\n// </copyright>\n\nusing System.Runtime.CompilerServices;\n";

        private const string DisableAutoCreationTemplate = "using Unity.Entities;\n\n[assembly: DisableAutoCreation]";
        private const string InternalAccessTemplate = "\n[assembly: InternalsVisibleTo(\"{0}\")]";

        private const string RootUIPath = "Packages/com.bovinelabs.core/Editor Default Resources/AssemblyBuilder/";
        private static readonly UITemplate AssemblyBuilderTemplate = new(RootUIPath + "AssemblyBuilder");
        private static readonly Func<string> GetActiveFolderPath;
        private static readonly Func<object> GetProjectBrowserIfExists;
        private static readonly FieldInfo ViewMode;

        static AssemblyBuilderWindow()
        {
            var projectWindowUtilType = typeof(ProjectWindowUtil);
            var getActiveFolderPathMethod = projectWindowUtilType.GetMethod("GetActiveFolderPath", BindingFlags.Static | BindingFlags.NonPublic);
            GetActiveFolderPath = (Func<string>)Delegate.CreateDelegate(typeof(Func<string>), getActiveFolderPathMethod!);

            var getProjectBrowserIfExistsMethod = projectWindowUtilType.GetMethod("GetProjectBrowserIfExists", BindingFlags.Static | BindingFlags.NonPublic);
            GetProjectBrowserIfExists = (Func<object>)Delegate.CreateDelegate(typeof(Func<object>), getProjectBrowserIfExistsMethod!);

            ViewMode = getProjectBrowserIfExistsMethod!.ReturnType.GetField("m_ViewMode", BindingFlags.Instance | BindingFlags.NonPublic)!;
        }

        [MenuItem(EditorMenus.RootMenuTools + "Assembly Builder", priority = 1007)]
        private static void ShowWindow()
        {
            // Get existing open window or if none, make a new one:
            var window = (AssemblyBuilderWindow)GetWindow(typeof(AssemblyBuilderWindow));
            window.titleContent = new GUIContent("Assembly Builder");
            window.Show();
        }

        private static string GetAssemblyInfoPath(string folder)
        {
            return $"{Directory.GetCurrentDirectory()}/{folder}/AssemblyInfo.cs";
        }

        private static string GetAssemblyInfoHeader()
        {
            return string.Format(AssemblyInfoTemplate, PlayerSettings.companyName);
        }

        private void Update()
        {
            this.rootVisualElement.Q<TextField>("directory").value = GetDirectory();
        }

        private static string GetDirectory()
        {
            var isTwoColumnView = IsTwoColumnView();

            if (!isTwoColumnView && Selection.objects.Length == 1)
            {
                var assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
                return AssetDatabase.IsValidFolder(assetPath) ? assetPath + "/" : Path.GetDirectoryName(assetPath)!.Replace("\\", "/");
            }

            return GetActiveFolderPath();
        }

        private static bool IsTwoColumnView()
        {
            var browser = GetProjectBrowserIfExists();
            if (browser == null)
            {
                return true;
            }

            var mode = ViewMode.GetValue(browser)!;
            return !Convert.ChangeType(mode, Enum.GetUnderlyingType(mode.GetType())).Equals(0);
        }

        private void OnEnable()
        {
            // Reference to the root of the window.
            var root = this.rootVisualElement;
            AssemblyBuilderTemplate.Clone(root);

            this.rootVisualElement.Query<Toggle>(className: "assembly").ForEach(this.BindAssemblyToggle);
            this.rootVisualElement.Q<Button>("create").clickable.clicked += this.Create;
            this.rootVisualElement.Q<TextField>("name").value = $"{PlayerSettings.companyName}.";
            this.rootVisualElement.Q<TextField>("directory").SetEnabled(false);

#if !UNITY_NETCODE
            foreach (var toggle in this.rootVisualElement.Q("referenceCommon").Children().OfType<Toggle>().ToList())
            {
                if (toggle.label is "Unity.NetCode" or "Unity.Networking.Transport")
                {
                    toggle.RemoveFromHierarchy();
                }
            }

            foreach (var toggle in this.rootVisualElement.Q("toggleCommon").Children().OfType<Toggle>().ToList())
            {
                if (toggle.label == "Server")
                {
                    toggle.RemoveFromHierarchy();
                }
            }
#endif
        }

        private void BindAssemblyToggle(Toggle toggle)
        {
            toggle.RegisterValueChangedCallback(evt =>
            {
                var foldout = this.rootVisualElement.Q<Foldout>($"reference{toggle.label}");
                if (foldout == null)
                {
                    return;
                }

                foldout.SetEnabled(evt.newValue);
                foldout.value = evt.newValue;
            });
        }

        private void Create()
        {
            var activeFolderPath = GetDirectory();

            var assemblyToggles = this.rootVisualElement.Query<Toggle>(className: "assembly").Where(t => t.value).ToList();

            // Sort so Data is first, Systems is second, so it can be added to other packages
            assemblyToggles.Sort((t1, t2) => t1.label == "Data" ? -1 :
                t2.label == "Data" ? 1 :
                t1.label is "Main" or "Server" ? -1 :
                t2.label is "Main" or "Server" ? 1 : 0);

            var nameField = this.rootVisualElement.Q<TextField>("name").value;

            if (string.IsNullOrWhiteSpace(nameField))
            {
                BLGlobalLogger.LogErrorString($"AssemblyName '{nameField}' is invalid");
                return;
            }

            var internalAccess = this.GetToggleValue("internalAccess");
            var disableAutoCreation = this.GetToggleValue("disableAutoCreation");
            var allowUnsafeCode = this.GetToggleValue("allowUnsafeCode");

            foreach (var toggle in assemblyToggles)
            {
                var label = toggle.label;
                var assemblyName = label == "Main" ? nameField : $"{nameField}.{label}";
                var folder = $"{activeFolderPath}/{assemblyName}";

                if (AssetDatabase.IsValidFolder(folder))
                {
                    BLGlobalLogger.LogErrorString($"MenuPath {folder} already exists");
                    continue;
                }

                var result = AssetDatabase.CreateFolder(activeFolderPath, assemblyName);
                if (string.IsNullOrWhiteSpace(result))
                {
                    BLGlobalLogger.LogErrorString($"Unable to create folder: {activeFolderPath} assembly name: {assemblyName}");
                    return;
                }

                var definition = AssemblyDefinitionTemplate.New();
                definition.name = assemblyName;
                definition.allowUnsafeCode = allowUnsafeCode;

                var references = this.rootVisualElement.Q<Foldout>($"reference{toggle.label}")?.Children().OfType<Toggle>().Select(t => t.label).ToList() ??
                    new List<string>();

                references.Add("BovineLabs.Core");
                references.Add("BovineLabs.Core.Extensions");

                // Also add our main references
                references.AddRange(this.GetCommonReferences());

                if (label == "Data")
                {
                    if (internalAccess)
                    {
                        this.AddInternalAccess(nameField, folder, "Data");
                    }
                }
                else
                {
                    // Add the data assembly
                    references.Add($"{nameField}.Data");

                    // Add the systems reference to other assemblies that isn't authoring or itself
                    if (label != "Main" && label != "System" && label != "Authoring")
                    {
                        references.Add($"{nameField}");

                        if (label == "Editor")
                        {
                            references.Add($"{nameField}.Authoring");
                        }
                    }
                    else
                    {
                        if (internalAccess)
                        {
                            if (label == "Main")
                            {
                                this.AddInternalAccess(nameField, folder, "Data", "Main", "Authoring");
                            }
                            else if (label == "Server")
                            {
                                this.AddInternalAccess(nameField, folder, "Data", "Main", "Server", "Authoring");
                            }
                            else if (label == "Authoring")
                            {
                                this.AddInternalAccess(nameField, folder, "Data", "Main", "Authoring", "Debug");
                            }
                        }
                    }

                    switch (label)
                    {
                        case "Main":
                            AddAnchor(references);
                            break;

                        case "Server":
                            definition.defineConstraints.Add("!UNITY_CLIENT");
                            break;

                        case "Debug":
                            AddAnchor(references);
                            definition.defineConstraints.Add("UNITY_EDITOR || BL_DEBUG");
                            break;

                        case "Authoring":
                            references.Add("BovineLabs.Core.Authoring");
                            references.Add("BovineLabs.Core.Extensions.Authoring");
                            definition.defineConstraints.Add("UNITY_EDITOR");
                            break;

                        case "Editor":
                        case "Tests":
                        {
                            // Limit to editor platform
                            definition.includePlatforms.Add("Editor");

                            // Add test requirements
                            if (label == "Tests")
                            {
                                definition.optionalUnityReferences.Add("TestAssemblies");

                                references.Add("BovineLabs.Testing");
                                references.Add("Unity.PerformanceTesting");

                                if (disableAutoCreation)
                                {
                                    var assemblyInfoPath = GetAssemblyInfoPath(folder);
                                    var text = GetAssemblyInfoHeader() + DisableAutoCreationTemplate;
                                    File.WriteAllText(assemblyInfoPath, text);
                                }
                            }
                            else if (label == "Editor")
                            {
                                references.Add("BovineLabs.Core.Editor");
                                references.Add("BovineLabs.Core.Extensions.Editor");
                            }

                            break;
                        }
                    }
                }

                // Sort alphabetical because it's nicer
                references.Sort();

                definition.references.AddRange(references);

                var json = EditorJsonUtility.ToJson(definition, true);
                var path = $"{folder}/{assemblyName}.asmdef";
                var asmPath = $"{Directory.GetCurrentDirectory()}/{path}";
                File.WriteAllText(asmPath, json);

                AssetDatabase.Refresh();
            }

            AssetDatabase.Refresh();
        }

        private static void AddAnchor(List<string> references)
        {
            references.Add("BovineLabs.Anchor");
            references.Add("Unity.AppUI");
            references.Add("Unity.AppUI.MVVM");
            references.Add("Unity.AppUI.Navigation");
        }

        private IEnumerable<string> GetCommonReferences()
        {
            return this.rootVisualElement.Q("referenceCommon").Children().OfType<Toggle>().Where(t => t.value).Select(t => t.label);
        }

        private bool GetToggleValue(string toggleName)
        {
            return this.rootVisualElement.Q<Toggle>(toggleName).value;
        }

        private void AddInternalAccess(string nameField, string folder, params string[] ignore)
        {
            var otherAssemblies =
                this
                    .rootVisualElement
                    .Query<Toggle>(className: "assembly")
                    .ToList()
                    .Where(t => !ignore.Contains(t.label))
                    .Select(t => (object)$"{(t.label == "Main" ? nameField : $"{nameField}.{t.label}")}")
                    .ToArray();

            var assemblyInfoPath = GetAssemblyInfoPath(folder);

            var internalAccessTemplate = GetAssemblyInfoHeader();
            foreach (var assembly in otherAssemblies.OrderBy(s => s))
            {
                internalAccessTemplate += string.Format(InternalAccessTemplate, assembly);
            }

            File.WriteAllText(assemblyInfoPath, internalAccessTemplate);
        }
    }
}
