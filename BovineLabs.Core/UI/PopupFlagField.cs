// <copyright file="PopupFlagField.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.UI
{
    using System;
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
            : this(string.Empty, Array.Empty<string>(), Array.Empty<int>())
        {
        }

        /// <summary> Initializes a new instance of the <see cref="PopupFlagField" /> class. </summary>
        /// <param name="label"> Label. </param>
        /// <param name="displayNames"> The display names. </param>
        /// <param name="defaultValue"> The default value. </param>
        public PopupFlagField(string label, string[] displayNames, IReadOnlyList<int> defaultValue)
            : base(label, displayNames, true)
        {
            this.rawValue = Array.Empty<int>();
            this.defaultValue = defaultValue;

            if (this.DisplayNames.Count > 0)
            {
                this.SetValueWithoutNotify(this.defaultValue);
            }

            if (this.rawValue.Count == 0)
            {

            }

            this.Menu.selectionChanged += this.MenuOnSelectionChange;
        }

        /// <inheritdoc />
        protected override void OnDisplayNamesChanged()
        {
            this.rawValue = Array.Empty<int>();
            this.SetValueWithoutNotify(this.defaultValue);
        }

        /// <inheritdoc />
        protected override string GetText()
        {
            return this.rawValue.Count switch
            {
                0 => this.NoneText,
                1 => this.DisplayNames[this.rawValue[0]],
                _ => this.rawValue.Count == this.DisplayNames.Count ? "[All]" : "[Multiple...]",
            };
        }

        /// <inheritdoc />
        protected override bool AreEquals(IReadOnlyList<int> t1, IReadOnlyList<int> t2)
        {
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

                (this.selected, this.selected1) = (this.selected1, this.selected);
            }
        }

        /// <summary> The factory for UI Builder. </summary>
        [Preserve]
        public new class UxmlFactory : UxmlFactory<PopupFlagField, UxmlTraits>
        {
        }
    }
}
