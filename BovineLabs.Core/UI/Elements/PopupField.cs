// <copyright file="PopupField.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_2023_3_OR_NEWER
#nullable disable
namespace BovineLabs.Core.UI
{
    using System;
    using System.Collections.Generic;
    using Unity.Properties;
    using UnityEngine;
    using UnityEngine.UIElements;

    /// <summary> Generic popup selection field. </summary>
    [UxmlElement]
    public partial class PopupField<T> : PopupFieldBase<T, T>
    {
        /// <summary> USS class name of elements of this type. </summary>
        public new const string ussClassName = "unity-popup-field";

        /// <summary> USS class name of labels in elements of this type. </summary>
        public new const string labelUssClassName = ussClassName + "__label";

        /// <summary> USS class name of input elements in elements of this type. </summary>
        public new const string inputUssClassName = ussClassName + "__input";

        protected const int PopupFieldDefaultIndex = -1;

        private static readonly BindingId IndexProperty = nameof(index);

        private Func<T, string> formatSelectedValueCallback;
        private Func<T, string> formatListItemCallback;
        private int indexValue = PopupFieldDefaultIndex;

        /// <summary>Initializes a new instance of the <see cref="PopupField{T}"/> class. </summary>
        public PopupField()
            : this(null)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="PopupField{T}"/> class. </summary>
        public PopupField(string label = null)
            : base(label)
        {
            this.AddToClassList(ussClassName);
            this.labelElement.AddToClassList(labelUssClassName);
            this.visualInput.AddToClassList(inputUssClassName);
            this.AutoCloseMenu = true;
        }

        /// <summary>Initializes a new instance of the <see cref="PopupField{T}"/> class. </summary>
        public PopupField(List<T> choices, T defaultValue, Func<T, string> formatSelectedValueCallback = null, Func<T, string> formatListItemCallback = null)
            : this(null, choices, defaultValue, formatSelectedValueCallback, formatListItemCallback)
        {
        }

        /// <summary> Initializes a new instance of the <see cref="PopupField{T}"/> class. </summary>
        public PopupField(string label, List<T> choices, T defaultValue, Func<T, string> formatSelectedValueCallback = null, Func<T, string> formatListItemCallback = null)
            : this(label)
        {
            if (defaultValue == null)
            {
                throw new ArgumentNullException(nameof(defaultValue));
            }

            this.choices = choices;
            if (!this.ChoicesRaw.Contains(defaultValue))
            {
                throw new ArgumentException(string.Format("Default value {0} is not present in the list of possible values", defaultValue));
            }

            this.SetValueWithoutNotify(defaultValue);

            this.FormatListItemCallback = formatListItemCallback;
            this.formatSelectedValueCallback = formatSelectedValueCallback;
        }

        /// <summary> Initializes a new instance of the <see cref="PopupField{T}"/> class. </summary>
        public PopupField(List<T> choices, int defaultIndex, Func<T, string> formatSelectedValueCallback = null, Func<T, string> formatListItemCallback = null)
            : this(null, choices, defaultIndex, formatSelectedValueCallback, formatListItemCallback)
        {
        }

        /// <summary> Initializes a new instance of the <see cref="PopupField{T}"/> class. </summary>
        public PopupField(string label, List<T> choices, int defaultIndex, Func<T, string> formatSelectedValueCallback = null, Func<T, string> formatListItemCallback = null)
            : this(label)
        {
            this.choices = choices;

            this.index = defaultIndex;

            this.FormatListItemCallback = formatListItemCallback;
            this.formatSelectedValueCallback = formatSelectedValueCallback;
        }

        public override List<T> choices
        {
            get => this.ChoicesRaw;
            set
            {
                this.ChoicesRaw = value ?? throw new ArgumentNullException(nameof(value));

                if (this.indexValue >= 0 && this.indexValue < this.ChoicesRaw.Count)
                {
                    base.value = this.ChoicesRaw[this.indexValue];
                }
                else
                {
                    base.value = default;
                }

                this.NotifyPropertyChanged(ChoicesProperty);
            }
        }

        /// <summary>
        /// Callback that provides a string representation used to display the selected value.
        /// </summary>
        public virtual Func<T, string> FormatSelectedValueCallback
        {
            get => this.formatSelectedValueCallback;
            set
            {
                this.formatSelectedValueCallback = value;
                this.TextElement.text = this.GetValueToDisplay();
            }
        }

        /// <summary>
        /// Callback that provides a string representation used to populate the popup menu.
        /// </summary>
        public virtual Func<T, string> FormatListItemCallback
        {
            get => this.formatListItemCallback;
            set => this.formatListItemCallback = value;
        }

        protected override string GetValueToDisplay()
        {
            if (this.formatSelectedValueCallback != null)
            {
                return this.formatSelectedValueCallback(this.value);
            }
            return (this.value != null) ? this.value.ToString() : string.Empty;
        }

        protected virtual string GetListItemToDisplay(T v)
        {
            if (this.formatListItemCallback != null)
            {
                return this.formatListItemCallback(v);
            }

            return (v != null && this.ChoicesRaw.Contains(v)) ? v.ToString() : string.Empty;
        }

        /// <inheritdoc />
        public override T value
        {
            get => base.value;
            set
            {
                var previousIndex = this.indexValue;
                this.indexValue = this.ChoicesRaw?.IndexOf(value) ?? -1;
                base.value = value;
                if (this.indexValue != previousIndex)
                {
                    this.NotifyPropertyChanged(IndexProperty);
                }
            }
        }

        public override void SetValueWithoutNotify(T newValue)
        {
            this.indexValue = this.ChoicesRaw?.IndexOf(newValue) ?? -1;
            base.SetValueWithoutNotify(newValue);
        }

        /// <summary>
        /// Gets or sets the currently selected index in the popup menu.
        /// Setting the index will update the ::ref::value field and send a property change notification.
        /// </summary>
        [CreateProperty]
        [UxmlAttribute("index")]
        public int index
        {
            get => this.indexValue;
            set
            {
                if (value != this.indexValue)
                {
                    this.indexValue = value;
                    if (this.indexValue >= 0 && this.indexValue < this.ChoicesRaw.Count)
                    {
                        base.value = this.ChoicesRaw[this.indexValue];
                    }
                    else
                    {
                        base.value = default;
                    }

                    this.NotifyPropertyChanged(IndexProperty);
                }
            }
        }

        [UxmlAttribute("value")]
        [HideInInspector, SerializeField] int valueOverride;

        protected override void AddMenuItems(GenericDropdownMenu menu)
        {
            if (menu == null)
            {
                throw new ArgumentNullException(nameof(menu));
            }

            if (this.ChoicesRaw == null)
            {
                return;
            }

            foreach (var item in this.ChoicesRaw)
            {
                bool isSelected = EqualityComparer<T>.Default.Equals(item, this.value);
                menu.AddItem(this.GetListItemToDisplay(item), isSelected, () => this.ChangeValueFromMenu(item));
            }
        }

        private void ChangeValueFromMenu(T menuItem)
        {
            this.showMixedValue = false;
            this.value = menuItem;
        }
    }
}
#endif
