// <copyright file="PackageElement.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#nullable disable

namespace BovineLabs.Core.Editor.Welcome
{
    using System;
    using System.Collections.Generic;
    using BovineLabs.Core.Editor.UI;
    using UnityEngine;
    using UnityEngine.UIElements;

    [UxmlElement]
    public partial class PackageElement : VisualElement
    {
        private static readonly UITemplate Template = new("Packages/com.bovinelabs.core/Editor Default Resources/WelcomeWindow/PackageElement");

        private readonly VisualElement packageRoot;
        private readonly Label titleLabel;
        private readonly Label descriptionLabel;
        private readonly Button installButton;
        private Clickable clickable;
        private string url;
        private string packageName;
        private string gitUrl;
        private string dependencies = string.Empty;
        private IReadOnlyList<string> dependencyList = Array.Empty<string>();
        private bool isInstallable;

        public PackageElement()
        {
            Template.Clone(this);

            this.packageRoot = this.Q<VisualElement>(className: "bl-library-card") ?? throw new InvalidOperationException("Missing library card root.");
            this.titleLabel = this.packageRoot.Q<Label>("Title") ?? throw new InvalidOperationException("Missing Title label.");
            this.descriptionLabel = this.packageRoot.Q<Label>("Description") ?? throw new InvalidOperationException("Missing Description label.");
            this.installButton = this.packageRoot.Q<Button>("InstallButton") ?? throw new InvalidOperationException("Missing InstallButton.");

            this.installButton.RegisterCallback<ClickEvent>(evt => evt.StopPropagation());
            this.SyncInstallable();
            this.SyncLink();

            this.installButton.clicked += () => this.InstallButtonClicked?.Invoke();
        }

        public enum InstallState : byte
        {
            Checking,
            Installing,
            Installed,
            Error,
            Ready,
        }

        public event Action InstallButtonClicked;

        [UxmlAttribute]
        public string Title
        {
            get => this.titleLabel.text;
            set => this.titleLabel.text = value;
        }

        [UxmlAttribute]
        public string Description
        {
            get => this.descriptionLabel.text;
            set => this.descriptionLabel.text = value;
        }

        [UxmlAttribute]
        public string Url
        {
            get => this.url;
            set
            {
                if (this.url != value)
                {
                    this.url = value;
                    this.SyncLink();
                }
            }
        }

        [UxmlAttribute]
        public string PackageName
        {
            get => this.packageName;
            set
            {
                if (this.packageName != value)
                {
                    this.packageName = value;
                    this.SyncInstallable();
                }
            }
        }

        [UxmlAttribute]
        public string GitUrl
        {
            get => this.gitUrl;
            set
            {
                if (this.gitUrl != value)
                {
                    this.gitUrl = value;
                    this.SyncInstallable();
                }
            }
        }

        [UxmlAttribute]
        public string Dependencies
        {
            get => this.dependencies;
            set
            {
                if (this.dependencies != value)
                {
                    this.dependencies = value;
                    this.dependencyList = ParseDependencies(value);
                }
            }
        }

        public bool IsInstallable => this.isInstallable;

        public IReadOnlyList<string> DependencyList => this.dependencyList;

        public void EnableInstallButton()
        {
            this.installButton.text = "Checking...";
            this.installButton.SetEnabled(false);
            this.installButton.EnableInClassList("bl-button--primary", false);
            this.installButton.EnableInClassList("bl-button--muted", true);
            this.installButton.style.display = DisplayStyle.Flex;
            this.installButton.tooltip = this.GitUrl;
        }

        public void UpdateInstallButton(InstallState state)
        {
            this.installButton.EnableInClassList("bl-button--muted", false);
            this.installButton.EnableInClassList("bl-button--primary", false);

            switch (state)
            {
                case InstallState.Installing:
                    this.installButton.text = "Installing...";
                    this.installButton.SetEnabled(false);
                    this.installButton.EnableInClassList("bl-button--primary", true);
                    break;
                case InstallState.Checking:
                    this.installButton.text = "Checking...";
                    this.installButton.SetEnabled(false);
                    this.installButton.EnableInClassList("bl-button--muted", true);
                    break;
                case InstallState.Installed:
                    this.installButton.text = "Installed";
                    this.installButton.SetEnabled(false);
                    this.installButton.EnableInClassList("bl-button--muted", true);
                    break;
                case InstallState.Error:
                    this.installButton.text = "Retry Install";
                    this.installButton.SetEnabled(true);
                    this.installButton.EnableInClassList("bl-button--primary", true);
                    break;
                case InstallState.Ready:
                    this.installButton.text = "Install";
                    this.installButton.SetEnabled(true);
                    this.installButton.EnableInClassList("bl-button--primary", true);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }

        private void SyncInstallable()
        {
            this.isInstallable = !string.IsNullOrWhiteSpace(this.PackageName) && !string.IsNullOrWhiteSpace(this.GitUrl);
            this.installButton.style.display = this.isInstallable ? DisplayStyle.Flex : DisplayStyle.None;
            this.installButton.tooltip = this.isInstallable ? this.GitUrl : string.Empty;
        }

        private void SyncLink()
        {
            if (this.clickable != null)
            {
                this.packageRoot.RemoveManipulator(this.clickable);
                this.clickable = null;
            }

            var hasUrl = !string.IsNullOrWhiteSpace(this.Url);
            this.packageRoot.EnableInClassList("bl-library-card--link", hasUrl);
            this.packageRoot.tooltip = hasUrl ? this.Url : string.Empty;
            this.packageRoot.focusable = hasUrl;

            if (!hasUrl)
            {
                return;
            }

            this.clickable = new Clickable(() => Application.OpenURL(this.Url));
            this.packageRoot.AddManipulator(this.clickable);
        }

        private static IReadOnlyList<string> ParseDependencies(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return Array.Empty<string>();
            }

            var dependenciesList = new List<string>();
            var splits = value.Split(';', StringSplitOptions.RemoveEmptyEntries);

            foreach (var split in splits)
            {
                var dependency = split.Trim();
                if (dependency.Length > 0)
                {
                    dependenciesList.Add(dependency);
                }
            }

            return dependenciesList;
        }
    }
}
