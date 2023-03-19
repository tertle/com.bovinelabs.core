// <copyright file="PopupField.cs" company="BovineLabs">
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

    /// <summary> A popup element. </summary>
    public sealed class PopupField : PopupFieldBase<int>
    {
        private readonly int defaultValue;

        public PopupField()
            : this(string.Empty, Array.Empty<string>(), 0)
        {
        }

        /// <summary> Initializes a new instance of the <see cref="PopupField" /> class. </summary>
        /// <param name="label"> Label. </param>
        /// <param name="displayNames"> The display names. </param>
        /// <param name="defaultValue"> The default value. </param>
        public PopupField(string label, string[] displayNames, int defaultValue)
            : base(label, displayNames, false)
        {
            this.rawValue = -1;
            this.defaultValue = defaultValue;

            if (this.DisplayNames.Count > 0)
            {
                this.SetValueWithoutNotify(defaultValue);
            }

            this.Menu.selectionChanged += this.MenuOnSelectionChange;
        }

        /// <inheritdoc />
        protected override void OnDisplayNamesChanged()
        {
            this.rawValue = -1;
            this.SetValueWithoutNotify(this.defaultValue);
        }

        /// <inheritdoc />
        protected override string GetText()
        {
            return (this.rawValue >= 0) && (this.rawValue < this.DisplayNames.Count) ? this.DisplayNames[this.rawValue] : "[None]";
        }

        /// <inheritdoc />
        protected override bool AreEquals(int t1, int t2)
        {
            return t1 == t2;
        }

        private void MenuOnSelectionChange(IEnumerable<object> obj)
        {
            var o = obj.FirstOrDefault();
            if (o == null)
            {
                this.value = -1;
            }
            else
            {
                this.value = this.DisplayNames.IndexOf(o.ToString());
            }

            this.HideMenu();
        }

        /// <summary> The factory for UI builder support. </summary>
        [Preserve]
        public new class UxmlFactory : UxmlFactory<PopupField, UxmlFactory.PopupUxmlTraits>
        {
            /// <summary> Custom traits for the factory. </summary>
            public class PopupUxmlTraits : UxmlTraits
            {
                private readonly UxmlStringAttributeDescription displayNames = new() { name = "display-names" };

                /// <inheritdoc />
                public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
                {
                    base.Init(ve, bag, cc);

                    var s = this.displayNames.GetValueFromBag(bag, cc);
                    var names = s.Split(',').Select(name => name.Trim());
                    ((PopupField)ve).SetDisplayNames(names);
                }
            }
        }
    }
}
