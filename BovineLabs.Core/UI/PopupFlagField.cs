// <copyright file="PopupFlagField.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.UI
{
    using System.Collections.Generic;
    using System.Linq;
    using BovineLabs.Core.Extensions;
    using UnityEngine.Scripting;
    using UnityEngine.UIElements;

    /// <summary> A popup flag element. </summary>
    public sealed class PopupFlagField : PopupFieldBase<IReadOnlyList<int>>
    {
        private readonly IReadOnlyList<int> defaultValue;
        private List<int> selected = new();
        private List<int> selected1 = new();

        /// <summary> Initializes a new instance of the <see cref="PopupFlagField" /> class. </summary>
        public PopupFlagField()
            : this(null, null)
        {
        }

        /// <summary> Initializes a new instance of the <see cref="PopupFlagField" /> class. </summary>
        /// ///
        /// <param name="displayNames"> The display names. </param>
        /// <param name="defaultValue"> The default value. </param>
        public PopupFlagField(string[] displayNames, IReadOnlyList<int> defaultValue = null)
            : this(null, displayNames, defaultValue)
        {
        }

        /// <summary> Initializes a new instance of the <see cref="PopupFlagField" /> class. </summary>
        /// <param name="label"> Label. </param>
        /// <param name="displayNames"> The display names. </param>
        /// <param name="defaultValue"> The default value. </param>
        public PopupFlagField(string label, string[] displayNames = null, IReadOnlyList<int> defaultValue = null)
            : base(label, displayNames, true)
        {
            this.rawValue = null;
            this.defaultValue = defaultValue ?? new int[0];

            if (this.DisplayNames != null && this.DisplayNames.Count > 0)
            {
                this.SetValueWithoutNotify(this.defaultValue);
            }

            this.Menu.selectionChanged += this.MenuOnSelectionChange;
        }

        /// <inheritdoc />
        protected override void OnDisplayNamesChanged()
        {
            this.rawValue = new int[0];
            this.SetValueWithoutNotify(this.defaultValue);
        }

        /// <inheritdoc />
        protected override void UpdateText(TextElement textElement)
        {
            if (this.rawValue.Count == 0)
            {
                textElement.text = this.NoneText;
            }
            else if (this.rawValue.Count == 1)
            {
                textElement.text = this.DisplayNames[this.rawValue[0]];
            }
            else if (this.rawValue.Count == this.DisplayNames.Count)
            {
                textElement.text = "[All]";
            }
            else
            {
                textElement.text = "[Multiple...]";
            }
        }

        /// <inheritdoc />
        protected override bool AreEquals(IReadOnlyList<int> t1, IReadOnlyList<int> t2)
        {
            if (t1 == null && t2 == null)
            {
                return true;
            }

            if (t1 == null || t2 == null)
            {
                return false;
            }

            return t1.SequenceEqual(t2);
        }

        private void MenuOnSelectionChange(IEnumerable<object> obj)
        {
            this.selected1.Clear();

            foreach (var o in obj)
            {
                var i = this.DisplayNames.IndexOf(o.ToString());
                this.selected1.Add(i);
            }

            if (!this.rawValue.SequenceEqual(this.selected1))
            {
                this.value = this.selected1;

                this.selected.Clear();

                var tmp = this.selected;
                this.selected = this.selected1;
                this.selected1 = tmp;
            }
        }

        /// <summary> The factory for UI Builder. </summary>
        [Preserve]
        public new class UxmlFactory : UxmlFactory<PopupFlagField, UxmlTraits>
        {
        }
    }
}
