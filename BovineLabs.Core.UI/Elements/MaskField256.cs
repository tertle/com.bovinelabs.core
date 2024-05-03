// <copyright file="MaskField256.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

// #nullable disable
namespace BovineLabs.Core.UI
{
    using System;
    using System.Collections.Generic;
    using BovineLabs.Core.Collections;
    using UnityEngine.UIElements;

    /// <summary> Make a field for masks. </summary>
    [UxmlElement]
    public partial class MaskField256 : MaskFieldBase<BitArray256>
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
        public MaskField256(List<string> choices, BitArray256 defaultMask, Func<string, string> formatSelectedValueCallback = null, Func<string, string> formatListItemCallback = null)
            : this(string.Empty, choices, defaultMask, formatSelectedValueCallback, formatListItemCallback)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="MaskField"/> class. Initializes and returns an instance of MaskField. </summary>
        /// <param name="label">The text to use as a label for the field.</param>
        /// <param name="choices">A list of choices to populate the field.</param>
        /// <param name="defaultMask">The initial mask value for this field.</param>
        /// <param name="formatSelectedValueCallback">A callback to format the selected value. Unity calls this method automatically when a new value is selected in the field..</param>
        /// <param name="formatListItemCallback">The initial mask value this field should use. Unity calls this method automatically when displaying choices for the field.</param>
        public MaskField256(string label, List<string> choices, BitArray256 defaultMask, Func<string, string> formatSelectedValueCallback = null, Func<string, string> formatListItemCallback = null)
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
        public MaskField256()
            : this(string.Empty)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="MaskField"/> class. Initializes and returns an instance of MaskField. </summary>
        /// <param name="label">The text to use as a label for the field.</param>
        public MaskField256(string label)
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

        protected override string GetListItemToDisplay(BitArray256 itemIndex)
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

        protected bool ShouldFormatListItem(BitArray256 itemIndex)
        {
            return itemIndex != BitArray256.None && itemIndex != BitArray256.All && this.FormatListItemCallback != null;
        }

        protected bool ShouldFormatSelectedValue()
        {
            return this.rawValue != BitArray256.None && this.rawValue != BitArray256.All && this.formatSelectedValueCallback != null && this.IsPowerOf2(this.rawValue);
        }

        protected override string GetDisplayedValue(BitArray256 itemIndex)
        {
            if (this.showMixedValue)
            {
                return mixedValueString;
            }

            var newValueToShowUser = "";

            if (itemIndex == BitArray256.None)
            {
                newValueToShowUser = this.ChoicesRaw[NothingIndex];
            }
            else if (itemIndex == BitArray256.All)
            {
                newValueToShowUser = this.ChoicesRaw[EverythingIndex];
            }
            else
            {
                // Show up the right selected value
                if (this.IsPowerOf2(itemIndex))
                {
                    var indexOfValue = 0;
                    while (new BitArray256((uint)indexOfValue) != itemIndex)
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

            if (maskFromItem == BitArray256.None)
            {
                newMask = BitArray256.None;
            }
            else if (maskFromItem == BitArray256.All)
            {
                newMask = BitArray256.All;
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
        protected override BitArray256 GetMaskValueOfItem(string item)
        {
            BitArray256 maskValue;
            var indexOfItem = this.ChoicesRaw.IndexOf(item);
            switch (indexOfItem)
            {
                case 0: // Nothing
                    maskValue = BitArray256.None;
                    break;
                case 1: // Everything
                    maskValue = BitArray256.All;
                    break;
                default: // All others
                    if (indexOfItem > 0)
                    {
                        maskValue = new BitArray256((uint)(indexOfItem - TotalIndex));
                    }
                    else
                    {
                        // If less than 0, it means the item was not found...
                        maskValue = BitArray256.None;
                    }

                    break;
            }
            return maskValue;
        }

        protected override bool IsItemSelected(BitArray256 maskOfItem)
        {
            var valueMask = this.value;

            if (maskOfItem == BitArray256.None)
                return valueMask == BitArray256.None;

            return (maskOfItem & valueMask) == maskOfItem;
        }

        protected override BitArray256 ComputeFullChoiceMask()
        {
            // Compute the full mask for all the items... it is not necessarily ~0 (which is all bits set to 1)
            if (this.choices.Count == 0)
            {
                return BitArray256.None;
            }

            if (this.choices.Count >= 256)
            {
                return BitArray256.All;
            }

            var b = new BitArray256();
            for (uint i = 0; i < this.choices.Count; i++)
            {
                b[i] = true;
            }

            return b;
        }

        protected override BitArray256 UpdateMaskIfEverything(BitArray256 currentMask)
        {
            var newMask = currentMask;
            // If the mask is full, put back the Everything flag.
            if (!this.IsNone(this.FullChoiceMask))
            {
                if ((currentMask & this.FullChoiceMask) == this.FullChoiceMask)
                {
                    newMask = BitArray256.All;
                }
                else
                {
                    newMask &= this.FullChoiceMask;
                }
            }

            return newMask;
        }

        protected override bool IsNone(BitArray256 itemIndex)
        {
            return itemIndex == BitArray256.None;
        }

        protected override bool IsEverything(BitArray256 itemIndex)
        {
            return itemIndex == BitArray256.All;
        }

        protected override bool IsPowerOf2(BitArray256 itemIndex)
        {
            return itemIndex.IsPowerOf2();
        }
    }
}
