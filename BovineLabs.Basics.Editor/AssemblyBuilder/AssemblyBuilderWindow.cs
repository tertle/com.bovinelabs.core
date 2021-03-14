// <copyright file="AssemblyBuilderWindow.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Basics.Editor.AssemblyBuilder
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using UnityEditor;
    using UnityEditorInternal;
    using UnityEngine;
    using UnityEngine.Assertions;
    using UnityEngine.UIElements;

    /// <summary> An editor window that allows easy creation of new assembly definitions. </summary>
    public class AssemblyBuilderWindow : EditorWindow
    {
        private const string UXMLDirectory = "Packages/com.bovinelabs.basics/BovineLabs.Basics.Editor/AssemblyBuilder/AssemblyBuilder.uxml";

        private const string AssemblyInfoTemplate =
            "// <copyright file=\"AssemblyInfo.cs\" company=\"{0}\">\n// Copyright (c) {0}. All rights reserved.\n// </copyright>\n\n";

        private const string DisableAutoCreationTemplate = "using Unity.Entities;\n\n[assembly: DisableAutoCreation]";

        private const string InternalAccessTemplate =
            "using System.Runtime.CompilerServices;\n\n[assembly: InternalsVisibleTo(\"{0}\")]\n[assembly: InternalsVisibleTo(\"{1}\")]\n[assembly: InternalsVisibleTo(\"{2}\")]";

        private static Func<string> getActiveFolderPath;

        private readonly Dictionary<string, string> assemblyNameToGUID = new Dictionary<string, string>();

        [MenuItem("BovineLabs/Assembly Builder _%&a")]
        private static void ShowWindow()
        {
            // Get existing open window or if none, make a new one:
            AssemblyBuilderWindow window = (AssemblyBuilderWindow)GetWindow(typeof(AssemblyBuilderWindow));
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

        private static string GetAssemblyInfoPath(string folder) => $"{System.IO.Directory.GetCurrentDirectory()}/{folder}/AssemblyInfo.cs";

        private static string GetAssemblyInfoTemplate() => string.Format(AssemblyInfoTemplate, PlayerSettings.companyName);

        private void OnEnable()
        {
            var assemblyDefinitions = AssetDatabase.FindAssets($"t:{nameof(AssemblyDefinitionAsset)}");
            foreach (var guid in assemblyDefinitions)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var fileName = Path.GetFileNameWithoutExtension(path);

                this.assemblyNameToGUID[fileName] = $"GUID:{guid}";
            }

            // Reference to the root of the window.
            var root = this.rootVisualElement;

            var assemblyBuilderVisualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UXMLDirectory);
            assemblyBuilderVisualTree.CloneTree(root);

            this.rootVisualElement.Query<Toggle>(className: "assembly").ForEach(this.BindAssemblyToggle);
            this.rootVisualElement.Q<Button>("create").clickable.clicked += this.Create;
            this.rootVisualElement.Q<TextField>("name").value = $"{PlayerSettings.companyName}.";
            this.rootVisualElement.Q<TextField>("directory").SetEnabled(false);
        }

        private void Update()
        {
            this.rootVisualElement.Q<TextField>("directory").value = GetActiveFolderPath();
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

            // Sort so Main is always first so it can be added to other packages
            assemblyToggles.Sort((t1, t2) => t1.label == "Main" ? -1 : t2.label == "Main" ? 1 : 0);

            var nameField = this.rootVisualElement.Q<TextField>("name").value;

            // TODO VALIDATE
            if (string.IsNullOrWhiteSpace(nameField))
            {
                Debug.LogError($"AssemblyName '{nameField}' is invalid");
                return;
            }

            var internalAccess = this.GetInternalAccess();
            var disableAutoCreation = this.GetDisableAutoCreation();

            foreach (var toggle in assemblyToggles)
            {
                var label = toggle.label;
                var assemblyName = label == "Main" ? nameField : $"{nameField}.{label}";
                var folder = $"{activeFolderPath}/{assemblyName}";

                if (AssetDatabase.IsValidFolder(folder))
                {
                    Debug.LogError($"MenuPath {folder} already exists");
                    continue;
                }

                AssetDatabase.CreateFolder(activeFolderPath, assemblyName);

                var definition = AssemblyDefinitionTemplate.New();
                definition.name = assemblyName;

                var references = this.rootVisualElement.Q<Foldout>($"reference{toggle.label}")?.Children().OfType<Toggle>().Select(t => t.label).ToList()
                                 ?? new List<string>();

                // Also add our main references
                references.AddRange(this.GetCommonReferences());

                if (label == "Main")
                {
                    if (internalAccess)
                    {
                        var otherAssemblies = this.rootVisualElement.Query<Toggle>(className: "assembly")
                                                  .ToList()
                                                  .Where(t => t.label != "Main")
                                                  .Select(t => (object)$"{nameField}.{t.label}")
                                                  .ToArray();

                        var assemblyInfoPath = GetAssemblyInfoPath(folder);
                        var internalAccessTemplate = string.Format(InternalAccessTemplate, otherAssemblies);
                        var text = GetAssemblyInfoTemplate() + internalAccessTemplate;
                        File.WriteAllText(assemblyInfoPath, text);
                    }
                }
                else
                {
                    // And the main assembly
                    definition.references.Add(nameField);

                    // Limit to editor platform
                    definition.includePlatforms.Add("Editor");

                    var isTest = label == "Tests";
                    var isPerformance = label == "PerformanceTests";

                    // Add test requirements
                    if (isTest || isPerformance)
                    {
                        definition.overrideReferences = true;
                        definition.precompiledReferences.Add("nunit.framework.dll");
                        definition.defineConstraints.Add("UNITY_INCLUDE_TESTS");

                        references.Add("BovineLabs.Testing");

                        if (isPerformance)
                        {
                            references.Add("Unity.PerformanceTesting");
                        }

                        if (disableAutoCreation)
                        {
                            var assemblyInfoPath = GetAssemblyInfoPath(folder);
                            var text = GetAssemblyInfoTemplate() + DisableAutoCreationTemplate;
                            File.WriteAllText(assemblyInfoPath, text);
                        }
                    }
                }

                // Sort alphabetical because it's nicer
                references.Sort();

                // Convert name to GUID, TODO search project
                definition.references.AddRange(references.Where(r => this.assemblyNameToGUID.ContainsKey(r)).Select(r => this.assemblyNameToGUID[r]));

                var json = EditorJsonUtility.ToJson(definition, true);

                // ReSharper disable once StringLiteralTypo
                var path = $"{folder}/{assemblyName}.asmdef";
                var asmPath = $"{System.IO.Directory.GetCurrentDirectory()}/{path}";
                File.WriteAllText(asmPath, json);

                AssetDatabase.Refresh();
                this.assemblyNameToGUID[assemblyName] = $"GUID:{AssetDatabase.AssetPathToGUID(path)}";
            }

            AssetDatabase.Refresh();
        }

        private IEnumerable<string> GetCommonReferences() => this.rootVisualElement.Q("referenceCommon").Children().OfType<Toggle>().Select(t => t.label);

        private bool GetInternalAccess() => this.rootVisualElement.Q<Toggle>("internalAccess").value;

        private bool GetDisableAutoCreation()
        {
            // TODO CHECK ENTITIES PACKAGE SELECTED
            return this.rootVisualElement.Q<Toggle>("disableAutoCreation").value;
        }
    }
}
