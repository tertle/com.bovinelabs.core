// <copyright file="BindableButton.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.UI
{
    using Unity.Properties;
    using UnityEngine.UIElements;

    [UxmlElement]
    public partial class BindableButton : Button
    {
        private bool wasClickedField;

        public BindableButton()
        {
            this.clicked += () => this.wasClicked = true;
        }

        [UxmlAttribute]
        [CreateProperty]
        public bool wasClicked
        {
            get
            {
                var value = this.wasClickedField;
                this.wasClickedField = false;
                return value;
            }

            set
            {
                if (!this.wasClickedField)
                {
                    this.wasClickedField = value;
                    this.NotifyPropertyChanged(nameof(this.wasClicked));
                }
            }
        }
    }
}
