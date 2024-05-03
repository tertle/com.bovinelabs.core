// <copyright file="BindableTextField.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.UI
{
    using Unity.Properties;
    using UnityEngine;
    using UnityEngine.UIElements;

    [UxmlElement]
    public partial class BindableTextField : TextField
    {
        private bool wasSubmitted;

        // default constructor required for codegen
        public BindableTextField()
            : this(null)
        {
        }

        public BindableTextField(string label = null, int maxLength = -1, bool multiline = false, bool isPasswordField = false, char maskChar = '*')
            : base(label, maxLength, multiline, isPasswordField, maskChar)
        {
        }

        [CreateProperty]
        [UxmlAttribute]
        public int caret
        {
            get => Mathf.Max(this.cursorIndex, this.selectIndex);
            set => this.SelectRange(value, value);
        }

        [UxmlAttribute]
        [CreateProperty]
        public bool submitted
        {
            get
            {
                var v = this.wasSubmitted;
                this.wasSubmitted = false;
                return v;
            }

            set
            {
                if (!this.wasSubmitted && value)
                {
                    this.wasSubmitted = true;
                    this.NotifyPropertyChanged(nameof(this.submitted));
                }
            }
        }

        [EventInterest(typeof(FocusOutEvent))]
        protected override void HandleEventBubbleUp(EventBase evt)
        {
            base.HandleEventBubbleUp(evt);

            if (evt is FocusOutEvent)
            {
                this.submitted = true;
            }
        }
    }
}
