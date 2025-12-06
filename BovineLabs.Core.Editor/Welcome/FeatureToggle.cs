// <copyright file="FeatureToggle.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Welcome
{
    using Unity.Properties;
    using UnityEngine.UIElements;

    [UxmlElement]
    public partial class FeatureToggle : Foldout
    {
        public static readonly string USSFeatureClassName = "bl-feature-toggle";
        public static readonly string FeatureToggleUssClassName = USSFeatureClassName + "__toggle";
        public static readonly string FeatureToggleDescriptionUssClassName = USSFeatureClassName + "__description";

        private readonly Toggle featureToggle;
        private readonly Label descriptionLabel;

        private bool featureEnabled = true;
        private string description = string.Empty;

        public FeatureToggle()
        {
            this.AddToClassList(USSFeatureClassName);

            var topGroup = new VisualElement();

            var foldoutToggle = this.Q<Toggle>();
            foldoutToggle.RemoveFromHierarchy();
            foldoutToggle.style.flexGrow = 1;

            this.hierarchy.Insert(0, topGroup);

            this.featureToggle = new Toggle { value = true };
            this.featureToggle.AddToClassList(FeatureToggleUssClassName);

            topGroup.Add(foldoutToggle);
            topGroup.Add(this.featureToggle);

            topGroup.style.flexDirection = FlexDirection.Row;

            this.descriptionLabel = new Label();
            this.descriptionLabel.AddToClassList(FeatureToggleDescriptionUssClassName);
            this.Add(this.descriptionLabel);
        }

        [UxmlAttribute]
        public string Define { get; set; } = string.Empty;

        [CreateProperty]
        [UxmlAttribute]
        public bool FeatureEnabled
        {
            get => this.featureEnabled;
            set
            {
                if (this.featureEnabled != value)
                {
                    this.featureEnabled = value;
                    this.featureToggle.value = value;
                }
            }
        }


        [UxmlAttribute]
        public string Description
        {
            get => this.description;
            set
            {
                if (this.description != value)
                {
                    this.description = value;
                    this.descriptionLabel.text = value;
                }
            }
        }

        public void SetFeatureEnabledWithoutNotify(bool newValue)
        {
            this.featureEnabled = newValue;
            this.featureToggle.SetValueWithoutNotify(newValue);
        }
    }
}
