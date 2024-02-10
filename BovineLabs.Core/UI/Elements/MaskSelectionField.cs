// <copyright file="MaskSelectionField.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_2023_3_OR_NEWER
#nullable disable
namespace BovineLabs.Core.UI
{
    using Unity.Properties;
    using UnityEngine.UIElements;

    [UxmlElement]
    public partial class MaskSelectionField : MaskField
    {
        /// <summary>Initializes a new instance of the <see cref="MaskSelectionField"/> class. Initializes and returns an instance of MaskField. </summary>
        public MaskSelectionField()
        {
            this.dropDownWidth = 150;
        }

        protected override string GetValueToDisplay()
        {
            return this.text;
        }

         [CreateProperty]
         [UxmlAttribute("text")]
         public new string text { get; set; }
    }
}
#endif
