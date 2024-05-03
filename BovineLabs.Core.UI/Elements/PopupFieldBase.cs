// <copyright file="PopupFieldBase.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.UI
{
    using System.Collections.Generic;
    using Unity.Properties;
    using UnityEngine;
    using UnityEngine.UIElements;

    /// <summary>
    /// This is the base class for all the popup field elements.
    /// TValue and TChoice can be different, see MaskField,
    ///   or the same, see PopupField
    /// </summary>
    /// <typeparam name="TValueType"> Used for the BaseField</typeparam>
    /// <typeparam name="TValueChoice"> Used for the choices list</typeparam>
    [UxmlElement]
    public abstract partial class PopupFieldBase<TValueType, TValueChoice> : BaseField<TValueType>
    {
        /// <summary> USS class name of elements of this type. </summary>
        public const string PopupFieldBaseUssClassName = "unity-base-popup-field";

        /// <summary> USS class name of text elements in elements of this type. </summary>
        public const string TextUssClassName = PopupFieldBaseUssClassName + "__text";

        /// <summary> USS class name of arrow indicators in elements of this type. </summary>
        public const string ArrowUssClassName = PopupFieldBaseUssClassName + "__arrow";

        /// <summary> USS class name of labels in elements of this type. </summary>
        public const string LabelUssClassName = PopupFieldBaseUssClassName + "__label";

        /// <summary> USS class name of input elements in elements of this type. </summary>
        public const string InputUssClassName = PopupFieldBaseUssClassName + "__input";

        internal static readonly BindingId ChoicesProperty = nameof(choices);
        internal static readonly BindingId TextProperty = nameof(text);

        private VisualElement arrowElement;

        protected readonly VisualElement visualInput;

        // Set this callback to provide a specific implementation of the menu.
        internal IGenericMenu GenericMenu;

        protected PopupFieldBase(string label)
            : this(new VisualElement { pickingMode = PickingMode.Ignore }, label)
        {
        }

        private PopupFieldBase(VisualElement visualElement, string label)
            : base(label, visualElement)
        {
            this.visualInput = visualElement;

            this.AddToClassList(PopupFieldBaseUssClassName);
            this.labelElement.AddToClassList(LabelUssClassName);

            this.TextElement = new PopupTextElement { pickingMode = PickingMode.Ignore };
            this.TextElement.AddToClassList(TextUssClassName);
            this.visualInput.AddToClassList(InputUssClassName);
            this.visualInput.Add(this.TextElement);

            this.arrowElement = new VisualElement();
            this.arrowElement.AddToClassList(ArrowUssClassName);
            this.arrowElement.pickingMode = PickingMode.Ignore;
            this.visualInput.Add(this.arrowElement);

            this.choices = new List<TValueChoice>();

            this.RegisterCallback<PointerDownEvent>(this.OnPointerDownEvent);
            this.RegisterCallback<PointerMoveEvent>(this.OnPointerMoveEvent);
            this.RegisterCallback<MouseDownEvent>(e =>
            {
                if (e.button == (int)MouseButton.LeftMouse)
                {
                    e.StopPropagation();
                }
            });
            this.RegisterCallback<NavigationSubmitEvent>(this.OnNavigationSubmit);
        }

        /// <summary> This is the text displayed to the user for the current selection of the popup. </summary>
        [CreateProperty(ReadOnly = true)]
        public string text => this.TextElement.text;

        /// <summary> The list of choices to display in the popup menu. </summary>
        [CreateProperty]
        public abstract List<TValueChoice> choices { get; set; }

        [CreateProperty]
        [UxmlAttribute("dropDownWidth")]
        public float dropDownWidth { get; set; }

        /// <summary> Gets this is the text displayed. </summary>
        protected TextElement TextElement { get; }

        protected List<TValueChoice> ChoicesRaw { get; set; }

        protected bool AutoCloseMenu { get; set; }

        /// <summary> Allow changing value without triggering any change event. </summary>
        /// <param name="newValue">The new value.</param>
        public override void SetValueWithoutNotify(TValueType newValue)
        {
            base.SetValueWithoutNotify(newValue);
            ((INotifyValueChanged<string>)this.TextElement).SetValueWithoutNotify(this.GetValueToDisplay());
        }

        // This method is used when the menu is built to fill up all the choices.
        protected abstract void AddMenuItems(IGenericMenu menu);

        protected override void UpdateMixedValueContent()
        {
            if (this.showMixedValue)
            {
                this.value = default;
                this.TextElement.text = mixedValueString;
            }

            this.TextElement.EnableInClassList(mixedValueLabelUssClassName, this.showMixedValue);
        }

        // This is the value to display to the user
        protected abstract string GetValueToDisplay();

        private void OnPointerDownEvent(PointerDownEvent evt)
        {
            this.ProcessPointerDown(evt);
        }

        private void OnPointerMoveEvent(PointerMoveEvent evt)
        {
            // Support cases where PointerMove corresponds to a MouseDown or MouseUp event with multiple buttons.
            if (evt.button == (int)MouseButton.LeftMouse)
            {
                if ((evt.pressedButtons & (1 << (int)MouseButton.LeftMouse)) != 0)
                {
                    this.ProcessPointerDown(evt);
                }
            }
        }

        private bool ContainsPointer(int pointerId)
        {
            var elementUnderPointer = this.GetTopElementUnderPointer(pointerId);
            return this == elementUnderPointer || this.visualInput == elementUnderPointer;
        }

        private void ProcessPointerDown<T>(PointerEventBase<T> evt)
            where T : PointerEventBase<T>, new()
        {
            if (evt.button == (int)MouseButton.LeftMouse)
            {
                if (this.ContainsPointer(evt.pointerId))
                {
                    this.schedule.Execute(this.ShowMenu);
                    evt.StopPropagation();
                }
            }
        }

        private void OnNavigationSubmit(NavigationSubmitEvent evt)
        {
            this.ShowMenu();
            evt.StopPropagation();
        }

        private void ShowMenu()
        {
            var elementPanel = this.GetElementPanel();

            var isPlayer = elementPanel?.contextType == ContextType.Player;
            if (isPlayer)
            {
                GenericDropdownMenu menu = new GenericDropdownMenu();

                menu.SetIsSingleSelectionDropdown(this.AutoCloseMenu);

                // TODO method or figure out a better way
                var menuContainer = menu.GetMenuContainer();

                var sheets = new HashSet<StyleSheet>();
                for (var i = 0; i < menuContainer.styleSheets.count; i++)
                {
                    sheets.Add(menuContainer.styleSheets[i]);
                }

                VisualElement e = this;
                do
                {
                    for (var i = 0; i < e.styleSheets.count; i++)
                    {
                        sheets.Add(e.styleSheets[i]);
                    }

                    e = e.parent;
                }
                while (e != null);

                foreach (var sheet in sheets)
                {
                    menuContainer.styleSheets.Add(sheet);
                }

                this.GenericMenu = new IGenericMenu(menu);
            }
            else
            {
                this.GenericMenu = DropdownUtility.CreateDropdown();
            }

            var bounds = this.visualInput.worldBound;
            if (this.dropDownWidth > 0)
            {
                bounds.width = this.dropDownWidth;
            }

            this.AddMenuItems(this.GenericMenu);
            this.GenericMenu.DropDown(bounds, this, true);
        }

        private class PopupTextElement : TextElement
        {
            protected override Vector2 DoMeasure(float desiredWidth, MeasureMode widthMode, float desiredHeight, MeasureMode heightMode)
            {
                var textToMeasure = this.text;
                if (string.IsNullOrEmpty(textToMeasure))
                {
                    textToMeasure = " ";
                }

                return this.MeasureTextSize(textToMeasure, desiredWidth, widthMode, desiredHeight, heightMode);
            }
        }
    }
}
