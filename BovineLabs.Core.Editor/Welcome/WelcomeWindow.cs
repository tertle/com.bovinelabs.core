// <copyright file="WelcomeWindow.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Welcome
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using BovineLabs.Core.Editor.Settings;
    using BovineLabs.Core.Editor.UI;
    using UnityEditor;
    using UnityEditor.PackageManager;
    using UnityEditor.PackageManager.Requests;
    using UnityEngine;
    using UnityEngine.UIElements;
    using EditorSettings = BovineLabs.Core.Editor.Settings.EditorSettings;

    [InitializeOnLoad]
    public class WelcomeWindow : EditorWindow
    {
        private const string ExtensionsEnableKey = "BL_CORE_EXTENSIONS";
        private const string PhysicsStatesDefine = "BL_DISABLE_PHYSICS_STATES";
        private const string PhysicsUpdateDefine = "BL_DISABLE_PHYSICS_ALWAYS_UPDATE";
        private const string ExtensionsDisabledClass = "bl-button--danger";
        private const string DiscordUrl = "https://discord.gg/2Y6eQ76AUV";
        private const string ReadmeUrl = "https://gitlab.com/tertle/com.bovinelabs.core/-/blob/master/README.md";
        private const string SessionStartupShownKey = "BovineLabs.Core.WelcomeWindow.StartupShown";
        private static readonly IReadOnlyList<string> Tabs = new[] { "Overview", "Extensions", "Packages" };
        private static readonly UITemplate Window = new("Packages/com.bovinelabs.core/Editor Default Resources/WelcomeWindow/WelcomeWindow");

        private readonly List<string> defines = new();
        private readonly List<string> initialDefines = new();
        private readonly List<FeatureEntry> featureToggles = new();
        private readonly List<PackageState> packages = new();
        private readonly Dictionary<string, PackageState> packageLookup = new(StringComparer.OrdinalIgnoreCase);

        private Button enableExtensionsButton = null!;
        private Button applyButton = null!;
        private ListRequest? packageListRequest;
        private bool extensionsSupported;
        private bool extensionsEnabled;
        private bool packageListUpdateRegistered;
        private bool installRequestUpdateRegistered;
        private bool hasPackageListResult;

        static WelcomeWindow()
        {
            EditorApplication.delayCall += () =>
            {
                var state = SessionState.GetBool(SessionStartupShownKey, false);
                if (state)
                {
                    return;
                }

                var preferences = WelcomePreferences.Get();
                if (preferences.AlwaysShowOnStartup || !preferences.WelcomePopupAlreadyShownOnce)
                {
                    SessionState.SetBool(SessionStartupShownKey, true);
                    ShowWindow();
                }
            };
        }

        [MenuItem(EditorMenus.RootMenu + "Manager", priority = -500)]
        private static void ShowWindow()
        {
            // Get existing open window or if none, make a new one:
            var window = GetWindow<WelcomeWindow>();
            window.titleContent = new GUIContent("BovineLabs");
            window.minSize = new Vector2(500, 600);
            window.Show();
        }

        private void OnEnable()
        {
            this.packageListRequest = null;

            this.defines.Clear();
            this.defines.AddRange(EditorSettingsUtility.GetSettings<EditorSettings>().ScriptingDefineSymbols);
            this.initialDefines.Clear();
            this.initialDefines.AddRange(this.defines);

            var preferences = WelcomePreferences.Get();
            preferences.WelcomePopupAlreadyShownOnce = true;

            var root = this.rootVisualElement;
            root.Clear();
            Window.Clone(root);
            root.dataSource = this;

            SetupTabs(root);
            this.SetupExtensions(root);
            SetupLinks(root);
            this.SetupPackages(root);
        }

        private void OnDisable()
        {
            if (this.packageListUpdateRegistered)
            {
                EditorApplication.update -= this.UpdatePackageList;
                this.packageListUpdateRegistered = false;
            }

            if (this.installRequestUpdateRegistered)
            {
                EditorApplication.update -= this.ProcessInstallRequests;
                this.installRequestUpdateRegistered = false;
            }

            this.packageListRequest = null;
            this.hasPackageListResult = false;

            foreach (var package in this.packages)
            {
                package.InstallRequest = null;
            }

            this.packages.Clear();
            this.packageLookup.Clear();
        }

        private static void SetupTabs(VisualElement root)
        {
            foreach (var tab in Tabs)
            {
                var button = root.Q<Button>($"Tab{tab}") ?? throw new InvalidOperationException($"Missing tab button Tab{tab}.");

                var tabName = tab;
                button.clicked += () => SetActiveTab(root, tabName);
            }

            SetActiveTab(root, Tabs[0]);
        }

        private static void SetActiveTab(VisualElement root, string tabName)
        {
            foreach (var tab in Tabs)
            {
                var button = root.Q<Button>($"Tab{tab}") ?? throw new InvalidOperationException($"Missing tab button Tab{tab}.");
                var panel = root.Q<VisualElement>($"Content{tab}") ?? throw new InvalidOperationException($"Missing tab content Content{tab}.");

                var isActive = tab == tabName;
                button.EnableInClassList("bl-tab--active", isActive);
                panel.EnableInClassList("bl-tab-panel--active", isActive);
                panel.style.display = isActive ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private void SetupExtensions(VisualElement root)
        {
            this.enableExtensionsButton = root.Q<Button>("EnableExtensions") ?? throw new InvalidOperationException("Missing EnableExtensions button.");
            this.applyButton = root.Q<Button>("ApplyChanges") ?? throw new InvalidOperationException("Missing ApplyChanges button.");

            this.featureToggles.Clear();

            foreach (var featureToggle in root.Query<FeatureToggle>().ToList())
            {
                if (string.IsNullOrWhiteSpace(featureToggle.Define))
                {
                    continue;
                }

                var toggle = featureToggle.Q<Toggle>(className: FeatureToggle.FeatureToggleUssClassName)
                             ?? throw new InvalidOperationException($"Missing toggle for feature {featureToggle.name}.");

                var entry = new FeatureEntry
                {
                    Define = featureToggle.Define,
                    FeatureToggle = featureToggle,
                    Toggle = toggle,
                };

                entry.Supported = IsFeatureSupported(entry.Define, out var unsupportedTooltip);

                if (!entry.Supported && !string.IsNullOrWhiteSpace(unsupportedTooltip))
                {
                    featureToggle.tooltip = unsupportedTooltip;
                    toggle.tooltip = unsupportedTooltip;
                }

                if (!entry.Supported && !this.defines.Contains(entry.Define))
                {
                    this.defines.Add(entry.Define);
                }

                toggle.RegisterValueChangedCallback(evt => this.OnFeatureToggled(entry, evt.newValue));

                this.featureToggles.Add(entry);
            }

            this.enableExtensionsButton.clicked += this.ToggleExtensions;
            this.applyButton.clicked += this.UpdateScriptingDefines;

            this.extensionsSupported = IsExtensionsSupported();

            this.enableExtensionsButton.SetEnabled(this.extensionsSupported);
            if (!this.extensionsSupported)
            {
                this.enableExtensionsButton.tooltip = "Extensions require Unity 6.3+";
            }

            this.extensionsEnabled = this.extensionsSupported && this.defines.Contains(ExtensionsEnableKey);
            this.UpdateExtensionsButtonText();
            this.SyncFeaturesToDefines(this.extensionsEnabled);
            this.UpdateApplyButtonState();
        }

        private static void SetupLinks(VisualElement root)
        {
            SetupButtonLink(root, "DiscordLink", DiscordUrl);
            SetupButtonLink(root, "DocsLink", ReadmeUrl);
        }

        private void SetupPackages(VisualElement root)
        {
            this.packages.Clear();
            this.packageLookup.Clear();

            foreach (var packageElement in root.Query<PackageElement>().ToList())
            {
                if (!packageElement.IsInstallable)
                {
                    continue;
                }

                var entry = new PackageState(packageElement);

                entry.Element.EnableInstallButton();
                packageElement.InstallButtonClicked += () => this.InstallPackage(entry);

                this.packages.Add(entry);
                this.packageLookup[entry.PackageName] = entry;
            }

            if (this.packages.Count > 0)
            {
                this.StartPackageListRequest();
            }
        }

        private static void SetupButtonLink(VisualElement root, string elementName, string url)
        {
            var button = root.Q<Button>(elementName) ?? throw new InvalidOperationException($"Missing link button {elementName}.");

            button.clicked += () => OpenLink(url);
            button.tooltip = url;
        }

        private static void OpenLink(string url)
        {
            Application.OpenURL(url);
        }

        private void StartPackageListRequest()
        {
            if (this.packageListRequest != null || this.packages.Count == 0)
            {
                return;
            }

            this.packageListRequest = Client.List();
            this.hasPackageListResult = false;

            if (!this.packageListUpdateRegistered)
            {
                EditorApplication.update += this.UpdatePackageList;
                this.packageListUpdateRegistered = true;
            }
        }

        private void UpdatePackageList()
        {
            if (this.packageListRequest == null || !this.packageListRequest.IsCompleted)
            {
                return;
            }

            this.hasPackageListResult = true;

            if (this.packageListRequest.Status == StatusCode.Success)
            {
                var installedPackages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var package in this.packageListRequest.Result)
                {
                    installedPackages.Add(package.name);
                }

                foreach (var package in this.packages)
                {
                    package.Installed = installedPackages.Contains(package.PackageName);
                    package.HadError = false;
                    this.UpdateInstallButton(package);
                }
            }
            else if (this.packageListRequest.Status >= StatusCode.Failure)
            {
                Debug.LogError($"Failed to list packages: {this.packageListRequest.Error.message}");

                foreach (var package in this.packages)
                {
                    package.HadError = false;
                    this.UpdateInstallButton(package);
                }
            }

            this.packageListRequest = null;

            EditorApplication.update -= this.UpdatePackageList;
            this.packageListUpdateRegistered = false;
        }

        private void InstallPackage(PackageState package)
        {
            if (package.InstallRequest != null || package.Installed)
            {
                return;
            }

            var installPlan = this.BuildInstallPlan(package);
            if (installPlan.Count == 0)
            {
                return;
            }

            var packagesToAdd = this.BuildPackageList(installPlan);
            if (packagesToAdd.Count == 0)
            {
                return;
            }

            var request = Client.AddAndRemove(packagesToAdd: packagesToAdd.ToArray());

            foreach (var install in installPlan)
            {
                install.HadError = false;
                install.InstallRequest = request;
                this.UpdateInstallButton(install);
            }

            if (!this.installRequestUpdateRegistered)
            {
                EditorApplication.update += this.ProcessInstallRequests;
                this.installRequestUpdateRegistered = true;
            }
        }

        private List<PackageState> BuildInstallPlan(PackageState package)
        {
            var installPlan = new List<PackageState>();
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            this.AddPackageAndDependencies(package, installPlan, visited);

            return installPlan;
        }

        private void AddPackageAndDependencies(PackageState package, List<PackageState> installPlan, HashSet<string> visited)
        {
            if (!visited.Add(package.PackageName))
            {
                return;
            }

            foreach (var dependencyName in package.Dependencies)
            {
                if (!this.packageLookup.TryGetValue(dependencyName, out var dependency))
                {
                    Debug.LogWarning($"Unable to find dependency \"{dependencyName}\" for {package.PackageName}.");
                    continue;
                }

                this.AddPackageAndDependencies(dependency, installPlan, visited);
            }

            if (package is { Installed: false, InstallRequest: null })
            {
                installPlan.Add(package);
            }
        }

        private List<string> BuildPackageList(List<PackageState> installPlan)
        {
            var packagesToAdd = new List<string>();
            var added = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var install in installPlan)
            {
                if (string.IsNullOrWhiteSpace(install.GitUrl))
                {
                    continue;
                }

                if (added.Add(install.GitUrl))
                {
                    packagesToAdd.Add(install.GitUrl);
                }
            }

            return packagesToAdd;
        }

        private void ProcessInstallRequests()
        {
            var hasPending = false;

            foreach (var package in this.packages)
            {
                var request = package.InstallRequest;
                if (request == null)
                {
                    continue;
                }

                if (!request.IsCompleted)
                {
                    hasPending = true;
                    continue;
                }

                if (request.Status == StatusCode.Success)
                {
                    package.Installed = true;
                }
                else
                {
                    package.Installed = false;
                    package.HadError = true;
                    Debug.LogError($"Failed to install {package.PackageName}: {request.Error.message}");
                }

                package.InstallRequest = null;
                this.UpdateInstallButton(package);
                this.StartPackageListRequest();
            }

            if (!hasPending)
            {
                EditorApplication.update -= this.ProcessInstallRequests;
                this.installRequestUpdateRegistered = false;
            }
        }

        private void UpdateInstallButton(PackageState package)
        {
            if (package.InstallRequest != null)
            {
                package.Element.UpdateInstallButton(PackageElement.InstallState.Installing);
            }
            else if (!this.hasPackageListResult)
            {
                package.Element.UpdateInstallButton(PackageElement.InstallState.Checking);
            }
            else if (package.Installed)
            {
                package.Element.UpdateInstallButton(PackageElement.InstallState.Installed);
            }
            else if (package.HadError)
            {
                package.Element.UpdateInstallButton(PackageElement.InstallState.Error);
            }
            else
            {
                package.Element.UpdateInstallButton(PackageElement.InstallState.Ready);
            }
        }

        private void ToggleExtensions()
        {
            if (!this.extensionsSupported)
            {
                return;
            }

            this.extensionsEnabled = !this.extensionsEnabled;

            if (this.extensionsEnabled)
            {
                if (!this.defines.Contains(ExtensionsEnableKey))
                {
                    this.defines.Add(ExtensionsEnableKey);
                }
            }
            else
            {
                this.defines.Remove(ExtensionsEnableKey);
            }

            this.UpdateExtensionsButtonText();
            this.SyncFeaturesToDefines(this.extensionsEnabled);
            this.UpdateApplyButtonState();
        }

        private void UpdateExtensionsButtonText()
        {
            this.enableExtensionsButton.text = this.extensionsEnabled ? "Extensions Enabled" : "Extensions Disabled";
            this.enableExtensionsButton.EnableInClassList(ExtensionsDisabledClass, !this.extensionsEnabled);
        }

        private void SyncFeaturesToDefines(bool extensionsOn)
        {
            foreach (var feature in this.featureToggles)
            {
                var featureEnabled = extensionsOn && feature.Supported && !this.defines.Contains(feature.Define);
                feature.FeatureToggle.SetFeatureEnabledWithoutNotify(featureEnabled);
                feature.Toggle.SetEnabled(extensionsOn && feature.Supported);
            }
        }

        private void OnFeatureToggled(FeatureEntry feature, bool enabled)
        {
            if (!feature.Supported)
            {
                feature.FeatureToggle.SetFeatureEnabledWithoutNotify(false);
                return;
            }

            feature.FeatureToggle.SetFeatureEnabledWithoutNotify(enabled);

            if (!this.extensionsEnabled)
            {
                return;
            }

            if (enabled)
            {
                this.defines.Remove(feature.Define);
            }
            else if (!this.defines.Contains(feature.Define))
            {
                this.defines.Add(feature.Define);
            }

            this.UpdateApplyButtonState();
        }

        private void UpdateScriptingDefines()
        {
            var settings = EditorSettingsUtility.GetSettings<EditorSettings>();

            var existingDefines = settings.ScriptingDefineSymbols;

            var add = new List<string>();
            var remove = new List<string>();

            foreach (var c in existingDefines)
            {
                if (!this.defines.Contains(c))
                {
                    remove.Add(c);
                }
            }

            foreach (var c in this.defines)
            {
                if (!existingDefines.Contains(c))
                {
                    add.Add(c);
                }
            }

            var so = new SerializedObject(settings);
            var property = so.FindProperty("scriptingDefineSymbols");

            foreach (var define in remove)
            {
                RemoveDefine(property, define);
            }

            foreach (var define in add)
            {
                AddDefine(property, define);
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            ScriptingDefineSymbolsEditor.ApplyDefinesToAll(add, remove);
            this.ResetInitialDefines();
            this.UpdateApplyButtonState();
        }

        private static void RemoveDefine(SerializedProperty property, string value)
        {
            for (var i = 0; i < property.arraySize; i++)
            {
                if (property.GetArrayElementAtIndex(i).stringValue == value)
                {
                    property.DeleteArrayElementAtIndex(i);
                    return;
                }
            }
        }

        private static void AddDefine(SerializedProperty property, string value)
        {
            for (var i = 0; i < property.arraySize; i++)
            {
                // Already exists
                if (property.GetArrayElementAtIndex(i).stringValue == value)
                {
                    return;
                }
            }

            property.arraySize++;
            property.GetArrayElementAtIndex(property.arraySize - 1).stringValue = value;
        }

        private void UpdateApplyButtonState()
        {
            var hasPendingChanges = this.HasPendingChanges();

            this.applyButton.SetEnabled(hasPendingChanges);
            this.applyButton.EnableInClassList("bl-button--primary", hasPendingChanges);
            this.applyButton.EnableInClassList("bl-button--muted", !hasPendingChanges);
        }

        private bool HasPendingChanges()
        {
            if (this.defines.Count != this.initialDefines.Count)
            {
                return true;
            }

            foreach (var define in this.defines)
            {
                if (!this.initialDefines.Contains(define))
                {
                    return true;
                }
            }

            return false;
        }

        private void ResetInitialDefines()
        {
            this.initialDefines.Clear();
            this.initialDefines.AddRange(this.defines);
        }

        private static bool IsExtensionsSupported()
        {
            var versionString = new string(Application.unityVersion.TakeWhile(c => char.IsDigit(c) || c == '.').ToArray());

            if (!Version.TryParse(versionString, out var version))
            {
                return false;
            }

            return version >= new Version(6000, 3);
        }

        private static bool IsFeatureSupported(string define, out string tooltip)
        {
            if (define is PhysicsStatesDefine or PhysicsUpdateDefine)
            {
#if !UNITY_PHYSICS
                tooltip = "Requires Unity Physics to enable Physics States.";
                return false;
#endif
            }

            tooltip = string.Empty;
            return true;
        }

        private class PackageState
        {
            public PackageState(PackageElement element)
            {
                this.Element = element;
            }

            public string PackageName => this.Element.PackageName;

            public string GitUrl => this.Element.GitUrl;

            public PackageElement Element { get; }

            public IReadOnlyList<string> Dependencies => this.Element.DependencyList;

            public bool Installed { get; set; }

            public AddAndRemoveRequest? InstallRequest { get; set; }

            public bool HadError { get; set; }
        }

        private class FeatureEntry
        {
            public string Define = string.Empty;

            public bool Supported = true;

            public FeatureToggle FeatureToggle = null!;

            public Toggle Toggle = null!;
        }
    }
}
