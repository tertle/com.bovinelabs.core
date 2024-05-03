// <copyright file="MaskFieldBase.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.UI
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Unity.Properties;
    using UnityEngine;
    using UnityEngine.Pool;
    using UnityEngine.UIElements;

    /// <summary>
    /// Base class implementing the shared functionality for editing bit mask values.
    /// </summary>
    [UxmlElement]
    public abstract partial class MaskFieldBase<TChoice> : PopupFieldBase<TChoice, string>
        where TChoice : unmanaged
    {
        protected const int NothingIndex = 0;
        protected const int EverythingIndex = 1;
        protected const int TotalIndex = 2;

        private const string MixedLabel = "Mixed...";
        private const string EverythingLabel = "Everything";
        private const string NothingLabel = "Nothing";

        // This is the list of string representing all the user choices
        private List<string> userChoices = new();

        // This is containing a mask to cover all the choices from the list. Computed with the help of m_UserChoicesMasks
        //     or based on the power of 2 mask values.
        protected TChoice FullChoiceMask { get; private set; }

        internal MaskFieldBase(string label)
            : base(label)
        {
            this.TextElement.RegisterCallback<GeometryChangedEvent>(this.OnTextElementGeometryChanged);
            this.AutoCloseMenu = false;
        }

        private void OnTextElementGeometryChanged(GeometryChangedEvent evt)
        {
            // Don't do anything for Nothing or Everything
            if (this.IsNone(this.value) || this.IsEverything(this.value))
            {
                return;
            }

            if (!this.IsPowerOf2(this.value))
            {
                // If the current text is "Mixed..." and we now have more space, we might need to check if the actual values would fit.
                // If the current label contains the actual values and we now have less space, we might need to change it to "Mixed..."
                if (this.TextElement.text == MixedLabel && evt.oldRect.width < evt.newRect.width
                    || this.TextElement.text != MixedLabel && evt.oldRect.width > evt.newRect.width)
                {
                    this.TextElement.text = this.GetMixedString();
                }
            }
        }

        // Can't use UxmlAttribute on an abstract method choices
        [CreateProperty]
        [UxmlAttribute("choices")]
        public List<string> choicesExposed
        {
            get => this.choices;
            set => this.choices = value;
        }

        /// <summary> The list of choices to display in the popup menu. </summary>
        [CreateProperty]
        public override List<string> choices
        {
            get => this.userChoices;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                // Keep the original list in a separate user list ...
                this.userChoices.Clear();
                this.userChoices.AddRange(value);

                // Now, add the nothing and everything choices...
                if (this.ChoicesRaw == null)
                {
                    this.ChoicesRaw = new List<string>();
                }
                else
                {
                    this.ChoicesRaw.Clear();
                }

                this.FullChoiceMask = this.ComputeFullChoiceMask();

                this.ChoicesRaw.Add(this.GetNothingName());
                this.ChoicesRaw.Add(this.GetEverythingName());
                this.ChoicesRaw.AddRange(this.userChoices);

                // Make sure to update the text displayed
                this.SetValueWithoutNotify(this.rawValue);
                this.NotifyPropertyChanged(ChoicesProperty);
            }
        }

        internal virtual string GetNothingName()
        {
            return NothingLabel;
        }

        internal virtual string GetEverythingName()
        {
            return EverythingLabel;
        }

        protected abstract TChoice ComputeFullChoiceMask();

        protected abstract bool IsNone(TChoice itemIndex);

        protected abstract bool IsEverything(TChoice itemIndex);

        protected abstract bool IsPowerOf2(TChoice itemIndex);

        protected virtual string GetListItemToDisplay(TChoice item)
        {
            return this.GetDisplayedValue(item);
        }

        protected abstract string GetDisplayedValue(TChoice itemIndex);

        protected string GetMixedString()
        {
            var sb = GenericPool<StringBuilder>.Get();

            foreach (var item in this.ChoicesRaw)
            {
                var maskOfItem = this.GetMaskValueOfItem(item);

                if (!this.IsItemSelected(maskOfItem))
                {
                    continue;
                }

                if (sb.Length > 0)
                {
                    sb.Append(", ");
                }

                sb.Append(item);
            }

            var mixedString = sb.ToString();
            var minSize = this.TextElement.MeasureTextSize(mixedString, 0, MeasureMode.Undefined, 0, MeasureMode.Undefined);

            // If text doesn't fit, we use "Mixed..."
            if (float.IsNaN(this.TextElement.resolvedStyle.width) || minSize.x > this.TextElement.resolvedStyle.width)
            {
                mixedString = MixedLabel;
            }

            sb.Clear();
            GenericPool<StringBuilder>.Release(sb);

            return mixedString;
        }

        public override void SetValueWithoutNotify(TChoice newValue)
        {
            base.SetValueWithoutNotify(this.UpdateMaskIfEverything(newValue));
        }

        protected override void AddMenuItems(IGenericMenu menu)
        {
            if (menu == null)
            {
                throw new ArgumentNullException(nameof(menu));
            }

            foreach (var item in this.ChoicesRaw)
            {
                var maskOfItem = this.GetMaskValueOfItem(item);
                var isSelected = this.IsItemSelected(maskOfItem);

                menu.AddItem(this.GetListItemToDisplay(maskOfItem), isSelected, () => this.ChangeValueFromMenu(item));
            }
        }

        protected abstract bool IsItemSelected(TChoice maskOfItem);

        protected void UpdateMenuItems()
        {
            var menu = (GenericDropdownMenu)this.GenericMenu;

            if (menu == null)
            {
                return;
            }

            foreach (var item in this.ChoicesRaw)
            {
                var maskOfItem = this.GetMaskValueOfItem(item);
                var isSelected = this.IsItemSelected(maskOfItem);

                menu.UpdateItem(this.GetListItemToDisplay(maskOfItem), isSelected);
            }
        }

        // Based on the current mask, this is updating the value of the actual mask to use vs the full mask.
        // This is returning ~0 if all the values are selected...
        protected abstract TChoice UpdateMaskIfEverything(TChoice currentMask);

        protected abstract void ChangeValueFromMenu(string menuItem);

        protected abstract TChoice GetMaskValueOfItem(string item);


    }
}
