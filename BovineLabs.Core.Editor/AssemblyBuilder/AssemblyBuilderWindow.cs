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
    using UnityEngine.Assertions;
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

        private static Func<string> getActiveFolderPath;

        [MenuItem("BovineLabs/Tools/Assembly Builder", priority = 1007)]
        private static void ShowWindow()
        {
            // Get existing open window or if none, make a new one:
            var window = (AssemblyBuilderWindow)GetWindow(typeof(AssemblyBuilderWindow));
            window.titleContent = new GUIContent("Assembly Builder");
            window.Show();
        }

        private static string GetActiveFolderPath()
        {
            if (getActiveFolderPath == null)
            {
                var projectWindowUtilType = typeof(ProjectWindowUtil);
                var methodInfo = projectWindowUtilType.GetMethod("GetActiveFolderPath", BindingFlags.Static | BindingFlags.NonPublic);
                Assert.IsNotNull(methodInfo);
                getActiveFolderPath = (Func<string>)Delegate.CreateDelegate(typeof(Func<string>), methodInfo);
                Assert.IsNotNull(getActiveFolderPath);
            }

            return getActiveFolderPath.Invoke();
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
            this.rootVisualElement.Q<TextField>("directory").value = GetActiveFolderPath();
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
            var activeFolderPath = GetActiveFolderPath();

            var assemblyToggles = this.rootVisualElement.Query<Toggle>(className: "assembly").Where(t => t.value).ToList();

            // Sort so Data is first, Systems is second, so it can be added to other packages
            assemblyToggles.Sort((t1, t2) => t1.label == "Data" ? -1
                : t2.label == "Data" ? 1
                : t1.label == "Systems" ? -1
                : t2.label == "Systems" ? 1 : 0);

            var nameField = this.rootVisualElement.Q<TextField>("name").value;

            if (string.IsNullOrWhiteSpace(nameField))
            {
                Debug.LogError($"AssemblyName '{nameField}' is invalid");
                return;
            }

            var internalAccess = this.GetToggleValue("internalAccess");
            var disableAutoCreation = this.GetToggleValue("disableAutoCreation");
            var allowUnsafeCode = this.GetToggleValue("allowUnsafeCode");

            foreach (var toggle in assemblyToggles)
            {
                var label = toggle.label;
                var assemblyName = $"{nameField}.{label}";
                var folder = $"{activeFolderPath}/{assemblyName}";

                if (AssetDatabase.IsValidFolder(folder))
                {
                    Debug.LogError($"MenuPath {folder} already exists");
                    continue;
                }

                AssetDatabase.CreateFolder(activeFolderPath, assemblyName);

                var definition = AssemblyDefinitionTemplate.New();
                definition.name = assemblyName;
                definition.allowUnsafeCode = allowUnsafeCode;

                var references = this.rootVisualElement.Q<Foldout>($"reference{toggle.label}")?.Children().OfType<Toggle>().Select(t => t.label).ToList() ?? new List<string>();
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
                    // And the data assembly
                    references.Add($"{nameField}.Data");

                    // Add the systems reference to other assemblies that isn't authoring or itself
                    if (label != "Systems" && label != "Authoring")
                    {
                        references.Add($"{nameField}.Systems");
                    }
                    else
                    {
                        if (internalAccess)
                        {
                            if (label == "System")
                            {
                                this.AddInternalAccess(nameField, folder, "Data", "Systems", "Authoring");
                            }
                            else if (label == "Authoring")
                            {
                                this.AddInternalAccess(nameField, folder, "Data", "Systems", "Authoring", "Debug");
                            }
                        }
                    }

                    switch (label)
                    {
                        case "Systems":
                            break;

                        case "Debug":
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
                                definition.overrideReferences = true;
                                definition.precompiledReferences.Add("nunit.framework.dll");
                                definition.defineConstraints.Add("UNITY_INCLUDE_TESTS");

                                references.Add("BovineLabs.Testing");
                                references.Add("UnityEditor.TestRunner");
                                references.Add("UnityEngine.TestRunner");
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

                definition.references.AddRange(references/*.Where(r => this.assemblyNameToGUID.ContainsKey(r)).Select(r => this.assemblyNameToGUID[r])*/);

                var json = EditorJsonUtility.ToJson(definition, true);

                // ReSharper disable once StringLiteralTypo
                var path = $"{folder}/{assemblyName}.asmdef";
                var asmPath = $"{Directory.GetCurrentDirectory()}/{path}";
                File.WriteAllText(asmPath, json);

                AssetDatabase.Refresh();
            }

            AssetDatabase.Refresh();
        }

        private IEnumerable<string> GetCommonReferences()
        {
            return this.rootVisualElement.Q("referenceCommon").Children().OfType<Toggle>().Select(t => t.label);
        }

        private bool GetToggleValue(string toggleName)
        {
            return this.rootVisualElement.Q<Toggle>(toggleName).value;
        }

        private void AddInternalAccess(string nameField, string folder, params string[] ignore)
        {
            var otherAssemblies = this.rootVisualElement.Query<Toggle>(className: "assembly")
                .ToList()
                .Where(t => ignore == null || !ignore.Contains(t.label))
                .Select(t => (object)$"{nameField}.{t.label}")
                .ToArray();

            var assemblyInfoPath = GetAssemblyInfoPath(folder);

            var internalAccessTemplate = GetAssemblyInfoHeader();
            foreach (var assembly in otherAssemblies.OrderBy(s => s))
            {
                internalAccessTemplate += string.Format(InternalAccessTemplate, assembly);
            }

            // var text = GetAssemblyInfoTemplate() + internalAccessTemplate;
            File.WriteAllText(assemblyInfoPath, internalAccessTemplate);
        }
    }
}
