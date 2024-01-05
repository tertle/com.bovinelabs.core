// <copyright file="MaskField.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#nullable disable
namespace BovineLabs.Core.UI
{
    using System;
    using System.Collections.Generic;
    using UnityEngine.UIElements;

    /// <summary> Make a field for masks. </summary>
    [UxmlElement]
    public partial class MaskField : MaskFieldBase<int>
    {
        /// <summary> USS class name of elements of this type. </summary>
        public const string MaskFieldUssClassName = "unity-mask-field";

        /// <summary> USS class name of labels in elements of this type. </summary>
        public const string MaskFieldLabelUssClassName = MaskFieldUssClassName + "__label";

        /// <summary> USS class name of input elements in elements of this type. </summary>
        public const string MaskFieldInputUssClassName = MaskFieldUssClassName + "__input";

        private Func<string, string> formatSelectedValueCallback;

        /// <summary>Initializes a new instance of the <see cref="MaskField"/> class. Initializes and returns an instance of MaskField. </summary>
        /// <param name="choices">A list of choices to populate the field.</param>
        /// <param name="defaultMask">The initial mask value for this field.</param>
        /// <param name="formatSelectedValueCallback">A callback to format the selected value. Unity calls this method automatically when a new value is selected in the field..</param>
        /// <param name="formatListItemCallback">The initial mask value this field should use. Unity calls this method automatically when displaying choices for the field.</param>
        public MaskField(List<string> choices, int defaultMask, Func<string, string> formatSelectedValueCallback = null, Func<string, string> formatListItemCallback = null)
            : this(null, choices, defaultMask, formatSelectedValueCallback, formatListItemCallback)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="MaskField"/> class. Initializes and returns an instance of MaskField. </summary>
        /// <param name="label">The text to use as a label for the field.</param>
        /// <param name="choices">A list of choices to populate the field.</param>
        /// <param name="defaultMask">The initial mask value for this field.</param>
        /// <param name="formatSelectedValueCallback">A callback to format the selected value. Unity calls this method automatically when a new value is selected in the field..</param>
        /// <param name="formatListItemCallback">The initial mask value this field should use. Unity calls this method automatically when displaying choices for the field.</param>
        public MaskField(string label, List<string> choices, int defaultMask, Func<string, string> formatSelectedValueCallback = null, Func<string, string> formatListItemCallback = null)
            : this(label)
        {
            this.choices = choices;
            this.FormatListItemCallback = formatListItemCallback;
            this.formatSelectedValueCallback = formatSelectedValueCallback;

            this.SetValueWithoutNotify(defaultMask);

            this.FormatListItemCallback = formatListItemCallback;
            this.FormatSelectedValueCallback = formatSelectedValueCallback;
        }

        /// <summary>Initializes a new instance of the <see cref="MaskField"/> class. Initializes and returns an instance of MaskField. </summary>
        public MaskField()
            : this(null)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="MaskField"/> class. Initializes and returns an instance of MaskField. </summary>
        /// <param name="label">The text to use as a label for the field.</param>
        public MaskField(string label)
            : base(label)
        {
            this.AddToClassList(MaskFieldUssClassName);
            this.labelElement.AddToClassList(MaskFieldLabelUssClassName);
            this.visualInput.AddToClassList(MaskFieldInputUssClassName);
        }

        /// <summary> Gets or sets callback that provides a string representation used to display the selected value. </summary>
        protected virtual Func<string, string> FormatSelectedValueCallback
        {
            get => this.formatSelectedValueCallback;
            set
            {
                this.formatSelectedValueCallback = value;
                this.TextElement.text = this.GetValueToDisplay();
            }
        }

        /// <summary> Gets or sets callback that provides a string representation used to populate the popup menu. </summary>
        protected virtual Func<string, string> FormatListItemCallback { get; set; }

        protected override string GetListItemToDisplay(int itemIndex)
        {
            string displayedValue = this.GetDisplayedValue(itemIndex);
            if (this.ShouldFormatListItem(itemIndex))
            {
                displayedValue = this.FormatListItemCallback(displayedValue);
            }

            return displayedValue;
        }

        protected override string GetValueToDisplay()
        {
            string displayedValue = this.GetDisplayedValue(this.rawValue);
            if (this.ShouldFormatSelectedValue())
            {
                displayedValue = this.formatSelectedValueCallback(displayedValue);
            }

            return displayedValue;
        }

        protected bool ShouldFormatListItem(int itemIndex)
        {
            return itemIndex != 0 && itemIndex != -1 && this.FormatListItemCallback != null;
        }

        protected bool ShouldFormatSelectedValue()
        {
            return this.rawValue != 0 && this.rawValue != -1 && this.formatSelectedValueCallback != null && this.IsPowerOf2(this.rawValue);
        }

        protected override string GetDisplayedValue(int itemIndex)
        {
            if (this.showMixedValue)
            {
                return mixedValueString;
            }

            var newValueToShowUser = "";

            switch (itemIndex)
            {
                case 0:
                    newValueToShowUser = this.ChoicesRaw[NothingIndex];
                    break;

                case ~0:
                    newValueToShowUser = this.ChoicesRaw[EverythingIndex];
                    break;

                default:
                    // Show up the right selected value
                    if (this.IsPowerOf2(itemIndex))
                    {
                        var indexOfValue = 0;
                        while ((1 << indexOfValue) != itemIndex)
                        {
                            indexOfValue++;
                        }

                        // To get past the Nothing + Everything choices...
                        indexOfValue += TotalIndex;
                        if (indexOfValue < this.ChoicesRaw.Count)
                        {
                            newValueToShowUser = this.ChoicesRaw[indexOfValue];
                        }
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(newValueToShowUser))
                        {
                            newValueToShowUser = this.GetMixedString();
                        }
                    }
                    break;
            }
            return newValueToShowUser;
        }

        protected override void ChangeValueFromMenu(string menuItem)
        {
            var newMask = this.value;
            var maskFromItem = this.GetMaskValueOfItem(menuItem);

            switch (maskFromItem)
            {
                // Nothing
                case 0:
                    newMask = 0;
                    break;

                // Everything
                case ~0:
                    newMask = ~0;
                    break;

                default:
                    // Make sure to have only the real selected one...
                    //newMask &= m_FullChoiceMask;

                    // Add or remove the newly selected...
                    if ((newMask & maskFromItem) == maskFromItem)
                    {
                        newMask &= ~maskFromItem;
                    }
                    else
                    {
                        newMask |= maskFromItem;
                    }

                    // If the mask is full, put back the Everything flag.
                    newMask = this.UpdateMaskIfEverything(newMask);
                    break;
            }
            // Finally, make sure to update the value of the mask...
            this.value = newMask;
            this.UpdateMenuItems();
        }

        // Returns the mask to be used for the item...
        protected override int GetMaskValueOfItem(string item)
        {
            int maskValue;
            var indexOfItem = this.ChoicesRaw.IndexOf(item);
            switch (indexOfItem)
            {
                case 0: // Nothing
                    maskValue = 0;
                    break;
                case 1: // Everything
                    maskValue = ~0;
                    break;
                default: // All others
                    if (indexOfItem > 0)
                    {
                        maskValue = 1 << (indexOfItem - TotalIndex);
                    }
                    else
                    {
                        // If less than 0, it means the item was not found...
                        maskValue = 0;
                    }

                    break;
            }
            return maskValue;
        }

        protected override bool IsItemSelected(int maskOfItem)
        {
            int valueMask = this.value;

            if (maskOfItem == 0)
                return valueMask == 0;

            return (maskOfItem & valueMask) == maskOfItem;
        }

        protected override int ComputeFullChoiceMask()
        {
            // Compute the full mask for all the items... it is not necessarily ~0 (which is all bits set to 1)
            if (this.choices.Count == 0)
            {
                return 0;
            }

            return this.choices.Count >= (sizeof(int) * 8) ? ~0 : (1 << this.choices.Count) - 1;
        }

        protected override int UpdateMaskIfEverything(int currentMask)
        {
            var newMask = currentMask;
            // If the mask is full, put back the Everything flag.
            if (!this.IsNone(this.FullChoiceMask))
            {
                if ((currentMask & this.FullChoiceMask) == this.FullChoiceMask)
                {
                    newMask = ~0;
                }
                else
                {
                    newMask &= this.FullChoiceMask;
                }
            }

            return newMask;
        }

        protected override bool IsNone(int itemIndex)
        {
            return itemIndex == 0;
        }

        protected override bool IsEverything(int itemIndex)
        {
            return itemIndex == ~0;
        }

        // Trick to get the number of selected values...
        // A power of 2 number means only 1 selected...
        protected override bool IsPowerOf2(int itemIndex)
        {
            return ((itemIndex & (itemIndex - 1)) == 0);
        }
    }
}
