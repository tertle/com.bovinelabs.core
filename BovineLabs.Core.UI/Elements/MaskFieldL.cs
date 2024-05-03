// <copyright file="MaskField.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

// #nullable disable
namespace BovineLabs.Core.UI
{
    using System;
    using System.Collections.Generic;
    using Unity.Mathematics;
    using UnityEngine.UIElements;

    /// <summary> Make a field for masks. </summary>
    [UxmlElement]
    public partial class MaskFieldL : MaskFieldBase<ulong>
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
        public MaskFieldL(List<string> choices, ulong defaultMask, Func<string, string> formatSelectedValueCallback = null, Func<string, string> formatListItemCallback = null)
            : this(string.Empty, choices, defaultMask, formatSelectedValueCallback, formatListItemCallback)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="MaskField"/> class. Initializes and returns an instance of MaskField. </summary>
        /// <param name="label">The text to use as a label for the field.</param>
        /// <param name="choices">A list of choices to populate the field.</param>
        /// <param name="defaultMask">The initial mask value for this field.</param>
        /// <param name="formatSelectedValueCallback">A callback to format the selected value. Unity calls this method automatically when a new value is selected in the field..</param>
        /// <param name="formatListItemCallback">The initial mask value this field should use. Unity calls this method automatically when displaying choices for the field.</param>
        public MaskFieldL(string label, List<string> choices, ulong defaultMask, Func<string, string> formatSelectedValueCallback = null, Func<string, string> formatListItemCallback = null)
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
        public MaskFieldL()
            : this(string.Empty)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="MaskField"/> class. Initializes and returns an instance of MaskField. </summary>
        /// <param name="label">The text to use as a label for the field.</param>
        public MaskFieldL(string label)
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

        protected override string GetListItemToDisplay(ulong itemIndex)
        {
            string displayedValue = this.GetDisplayedValue(itemIndex);
            if (this.ShouldFormatListItem(itemIndex))
            {
                displayedValue = this.FormatListItemCallback!(displayedValue);
            }

            return displayedValue;
        }

        protected override string GetValueToDisplay()
        {
            string displayedValue = this.GetDisplayedValue(this.rawValue);
            if (this.ShouldFormatSelectedValue())
            {
                displayedValue = this.formatSelectedValueCallback!(displayedValue);
            }

            return displayedValue;
        }

        protected bool ShouldFormatListItem(ulong itemIndex)
        {
            return itemIndex != 0 && itemIndex != ulong.MaxValue && this.FormatListItemCallback != null;
        }

        protected bool ShouldFormatSelectedValue()
        {
            return this.rawValue != 0 && this.rawValue != ulong.MaxValue && this.formatSelectedValueCallback != null && this.IsPowerOf2(this.rawValue);
        }

        protected override string GetDisplayedValue(ulong itemIndex)
        {
            if (this.showMixedValue)
            {
                return mixedValueString;
            }

            var newValueToShowUser = "";

            if (itemIndex == 0)
            {
                newValueToShowUser = this.ChoicesRaw[NothingIndex];
            }
            else if (itemIndex == ulong.MaxValue)
            {
                newValueToShowUser = this.ChoicesRaw[EverythingIndex];
            }
            else
            {
                // Show up the right selected value
                if (this.IsPowerOf2(itemIndex))
                {
                    var indexOfValue = 0;
                    while ((1UL << indexOfValue) != itemIndex)
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
            }

            return newValueToShowUser;
        }

        protected override void ChangeValueFromMenu(string menuItem)
        {
            var newMask = this.value;
            var maskFromItem = this.GetMaskValueOfItem(menuItem);

            if (maskFromItem == 0)
            {
                newMask = 0;
            }
            else if (maskFromItem == ulong.MaxValue)
            {
                newMask = ulong.MaxValue;
            }
            else
            {
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
            }

            // Finally, make sure to update the value of the mask...
            this.value = newMask;
            this.UpdateMenuItems();
        }

        // Returns the mask to be used for the item...
        protected override ulong GetMaskValueOfItem(string item)
        {
            ulong maskValue;
            var indexOfItem = this.ChoicesRaw.IndexOf(item);
            switch (indexOfItem)
            {
                case 0: // Nothing
                    maskValue = 0;
                    break;
                case 1: // Everything
                    maskValue = ulong.MaxValue;
                    break;
                default: // All others
                    if (indexOfItem > 0)
                    {
                        maskValue = 1UL << (indexOfItem - TotalIndex);
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

        protected override bool IsItemSelected(ulong maskOfItem)
        {
            var valueMask = this.value;

            if (maskOfItem == 0)
                return valueMask == 0;

            return (maskOfItem & valueMask) == maskOfItem;
        }

        protected override ulong ComputeFullChoiceMask()
        {
            // Compute the full mask for all the items... it is not necessarily ~0 (which is all bits set to 1)
            if (this.choices.Count == 0)
            {
                return 0;
            }

            if (this.choices.Count >= 64)
            {
                return ulong.MaxValue;
            }

            ulong b = 0UL;
            for (var i = 0; i < this.choices.Count; i++)
            {
                b |= 1ul << i;
            }

            return b;
        }

        protected override ulong UpdateMaskIfEverything(ulong currentMask)
        {
            var newMask = currentMask;
            // If the mask is full, put back the Everything flag.
            if (!this.IsNone(this.FullChoiceMask))
            {
                if ((currentMask & this.FullChoiceMask) == this.FullChoiceMask)
                {
                    newMask = ulong.MaxValue;
                }
                else
                {
                    newMask &= this.FullChoiceMask;
                }
            }

            return newMask;
        }

        protected override bool IsNone(ulong itemIndex)
        {
            return itemIndex == 0;
        }

        protected override bool IsEverything(ulong itemIndex)
        {
            return itemIndex == ulong.MaxValue;
        }

        protected override bool IsPowerOf2(ulong itemIndex)
        {
            return math.countbits(itemIndex) == 1;
        }
    }
}
