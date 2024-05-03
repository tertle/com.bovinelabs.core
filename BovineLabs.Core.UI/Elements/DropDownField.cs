// <copyright file="DropDownField.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.UI
{
    using System;
    using System.Collections.Generic;
    using Unity.Properties;
    using UnityEngine.UIElements;

    /// <summary>
    /// A control that allows the user to pick a choice from a list of options. For more information, refer to [[wiki:UIE-uxml-element-dropdown|UXML element Dropdown]].
    /// </summary>
    [UxmlElement]
    public partial class DropdownField : PopupField<string>
    {
        /// <summary>Initializes a new instance of the <see cref="DropdownField"/> class. </summary>
        public DropdownField()
            : this(null)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="DropdownField"/> class. </summary>
        public DropdownField(string label)
            : base(label)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="DropdownField"/> class. </summary>
        public DropdownField(
            List<string> choices,
            string defaultValue,
            Func<string, string> formatSelectedValueCallback = null,
            Func<string, string> formatListItemCallback = null)
            : this(null, choices, defaultValue, formatSelectedValueCallback, formatListItemCallback)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="DropdownField"/> class. </summary>
        public DropdownField(string label,
            List<string> choices,
            string defaultValue,
            Func<string, string> formatSelectedValueCallback = null,
            Func<string, string> formatListItemCallback = null)
            : base(label, choices, defaultValue, formatSelectedValueCallback, formatListItemCallback)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="DropdownField"/> class. </summary>
        public DropdownField(List<string> choices,
            int defaultIndex,
            Func<string, string> formatSelectedValueCallback = null,
            Func<string, string> formatListItemCallback = null)
            : this(null, choices, defaultIndex, formatSelectedValueCallback, formatListItemCallback)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="DropdownField"/> class. </summary>
        public DropdownField(string label,
            List<string> choices,
            int defaultIndex,
            Func<string, string> formatSelectedValueCallback = null,
            Func<string, string> formatListItemCallback = null)
            : base(label, choices, defaultIndex, formatSelectedValueCallback, formatListItemCallback)
        {
        }

        [CreateProperty]
        [UxmlAttribute("choices")]
        public List<string> choicesExposed
        {
            get => this.choices;
            set => this.choices = value;
        }
    }
}
