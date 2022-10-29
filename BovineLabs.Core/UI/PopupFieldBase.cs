// <copyright file="PopupFieldBase.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.UI
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UIElements;

    /// <summary> Base class for popup fields. </summary>
    /// <typeparam name="T"> The fields value. </typeparam>
    public abstract class PopupFieldBase<T> : BaseField<T>
    {
        public const int ItemHeight = 18; // TODO expose

        private const string UssClassName = "bl-popup-field";
        private const string LabelUssClassName = UssClassName + "__label";
        private const string InputUssClassName = UssClassName + "__input";
        private const string TextUssClassName = UssClassName + "__text";
        private const string ArrowUssClassName = UssClassName + "__arrow";
        private const string MenuUssClassName = UssClassName + "__menu";
        private readonly List<string> displayNames = new();

        private readonly VisualElement visualInput;
        private readonly TextElement textElement;

        private string noneText = "[None]";

        /// <summary> Initializes a new instance of the <see cref="PopupFieldBase{T}" /> class. </summary>
        /// <param name="label"> Label. </param>
        /// <param name="displayNames"> The display names. </param>
        /// <param name="multiSelect"> Is multi select supported. </param>
        protected PopupFieldBase(string label, string[] displayNames, bool multiSelect)
            : this(new VisualElement { pickingMode = PickingMode.Ignore }, label, displayNames, multiSelect)
        {
        }

        private PopupFieldBase(VisualElement visualElement, string label, string[] displayNames, bool multiSelect)
            : base(label, visualElement)
        {
            this.visualInput = visualElement;

            this.labelElement.AddToClassList(LabelUssClassName);

            SetPopupStyle(this);
            SetInputStyle(this.visualInput);

            this.textElement = CreateText();
            this.visualInput.Add(this.textElement);

            var arrow = CreateArrow();
            this.visualInput.Add(arrow);

            if (displayNames != null)
            {
                this.displayNames.AddRange(displayNames);
            }

            this.Menu = CreateMenu(this.displayNames, multiSelect);
        }

        /// <summary> Gets the text value. </summary>
        public string Text => this.textElement.text;

        /// <summary> Gets or sets the none text. </summary>
        public string NoneText
        {
            get => this.noneText;
            set
            {
                if (this.noneText == value)
                {
                    return;
                }

                this.noneText = value;
                this.UpdateText(this.textElement);
            }
        }

        /// <summary> Gets the display names. To set, use <see cref="SetDisplayNames" />. </summary>
        public IReadOnlyList<string> DisplayNames => this.displayNames;

        /// <summary> Gets the menu element. </summary>
        public ListView Menu { get; }

        /// <summary> Sets the display names. </summary>
        /// <param name="names"> The names to display. </param>
        public void SetDisplayNames(IEnumerable<string> names)
        {
            this.displayNames.Clear();
            this.displayNames.AddRange(names);
            this.OnDisplayNamesChanged();
        }

        /// <inheritdoc />
        public override void SetValueWithoutNotify(T newValue)
        {
            if (!this.AreEquals(this.rawValue, newValue))
            {
                base.SetValueWithoutNotify(newValue);
                this.UpdateText(this.textElement);
            }
        }

        /// <summary> Implement how to handle display names. </summary>
        protected abstract void OnDisplayNamesChanged();

        /// <summary> Updating of text requested. </summary>
        /// <param name="textElement"> The text element. </param>
        protected abstract void UpdateText(TextElement textElement);

        /// <summary> Implement how equality should be handled. </summary>
        /// <param name="t1"> The first value to compare. </param>
        /// <param name="t2"> The second value to compare. </param>
        /// <returns> True if equal. </returns>
        protected abstract bool AreEquals(T t1, T t2);

        /// <inheritdoc />
        protected override void ExecuteDefaultActionAtTarget(EventBase evt)
        {
            base.ExecuteDefaultActionAtTarget(evt);

            if (evt == null)
            {
                return;
            }

            var showMenu = false;
            var hideMenu = false;
            if (evt is KeyDownEvent kde)
            {
                if (kde.keyCode == KeyCode.Space || kde.keyCode == KeyCode.KeypadEnter || kde.keyCode == KeyCode.Return)
                {
                    showMenu = true;
                }
                else if (kde.keyCode == KeyCode.Escape)
                {
                    hideMenu = true;
                }
            }
            else if ((evt as MouseDownEvent)?.button == (int)MouseButton.LeftMouse)
            {
                var mde = (MouseDownEvent)evt;
                if (this.visualInput.ContainsPoint(this.visualInput.WorldToLocal(mde.mousePosition)))
                {
                    showMenu = true;
                }
            }

            if (showMenu)
            {
                this.ToggleMenu();
                evt.StopPropagation();
            }
            else if (hideMenu)
            {
                this.HideMenu();
                evt.StopPropagation();
            }
        }

        /// <summary> Hides the menu. </summary>
        protected void HideMenu()
        {
            if (this.Menu.parent == null)
            {
                return;
            }

            this.Menu.parent.UnregisterCallback<MouseDownEvent>(this.MenuNotClicked, TrickleDown.TrickleDown);
            this.Menu.parent.UnregisterCallback<MouseDownEvent>(this.MenuNotClicked);
            this.Menu.parent.Remove(this.Menu);
        }

        private static void SetPopupStyle(VisualElement element)
        {
            element.AddToClassList(UssClassName);

            element.style.marginLeft = 0;
            element.style.marginRight = 0;
            element.style.marginTop = 0;
            element.style.marginBottom = 0;

            element.style.paddingLeft = 0;
            element.style.paddingRight = 0;
            element.style.paddingTop = 0;
            element.style.paddingBottom = 0;

            element.style.height = 22;
            element.style.minWidth = 120;
        }

        private static void SetInputStyle(VisualElement element)
        {
            element.AddToClassList(InputUssClassName);

            element.style.flexDirection = FlexDirection.Row;
            element.style.paddingLeft = 3;
            element.style.paddingRight = 3;
            element.style.paddingTop = 0;
            element.style.paddingBottom = 2;

            element.style.borderBottomLeftRadius = 3;
            element.style.borderBottomRightRadius = 3;
            element.style.borderTopLeftRadius = 3;
            element.style.borderTopRightRadius = 3;

            element.style.borderLeftWidth = 1;
            element.style.borderRightWidth = 1;
            element.style.borderTopWidth = 1;
            element.style.borderBottomWidth = 1;

            element.style.unityTextAlign = TextAnchor.MiddleLeft;
            element.style.cursor = StyleKeyword.Initial;
        }

        private static TextElement CreateText()
        {
            var textElement = new TextElement();
            textElement.AddToClassList(TextUssClassName);
            textElement.pickingMode = PickingMode.Ignore;

            textElement.style.marginTop = 1;
            textElement.style.overflow = Overflow.Hidden;
            textElement.style.unityTextAlign = TextAnchor.MiddleLeft;
            textElement.style.whiteSpace = WhiteSpace.NoWrap;
            textElement.style.flexGrow = 1;

            return textElement;
        }

        private static VisualElement CreateArrow()
        {
            var arrowElement = new VisualElement();
            arrowElement.AddToClassList(ArrowUssClassName);
            arrowElement.pickingMode = PickingMode.Ignore;
            arrowElement.style.height = 12;
            arrowElement.style.width = 12;
            arrowElement.style.alignSelf = Align.Center;

            var arrow = Resources.Load<Texture2D>("ic_arrow_drop_down_white_48dp");
            arrowElement.style.backgroundImage = Background.FromTexture2D(arrow);
            return arrowElement;
        }

        private static ListView CreateMenu(List<string> displayNames, bool multiSelect)
        {
            var menu = new ListView { fixedItemHeight = ItemHeight };
            menu.AddToClassList(MenuUssClassName);

            VisualElement MakeItem()
            {
                return new Label();
            }

            void BindItem(VisualElement e, int i)
            {
                ((Label)e).text = displayNames[i];
            }

            displayNames.Add("Default");

            menu.itemsSource = displayNames;
            menu.makeItem = MakeItem;
            menu.bindItem = BindItem;
            menu.selectionType = multiSelect ? SelectionType.Multiple : SelectionType.Single;

            menu.style.position = Position.Absolute;
            menu.style.maxHeight = 200;
            menu.style.minHeight = 0;

            return menu;
        }

        private static void CopyStylesTo(VisualElement visualElement, VisualElement targetVisualElement)
        {
            if (visualElement.hierarchy.parent != null)
            {
                CopyStylesTo(visualElement.hierarchy.parent, targetVisualElement);
            }

            var sheets = visualElement.styleSheets;

            for (var i = 0; i < sheets.count; i++)
            {
                var sheet = sheets[i];
                targetVisualElement.styleSheets.Add(sheet);
            }
        }

        private void ToggleMenu()
        {
            if (this.DisplayNames.Count == 0)
            {
                return;
            }

            // Already shown
            if (this.Menu.parent != null)
            {
                this.HideMenu();
                return;
            }

            var root = this.GetRoot();

            this.Menu.style.minWidth = this.worldBound.width;
            this.Menu.style.left = this.worldBound.xMin;
            this.Menu.style.top = this.worldBound.yMax;
            this.Menu.fixedItemHeight = ItemHeight;
            this.Menu.style.height = this.DisplayNames.Count * ItemHeight + 1;

            this.Menu.styleSheets.Clear();
            this.Menu.Rebuild();

            CopyStylesTo(this, this.Menu);

            root.Add(this.Menu);
            root.RegisterCallback<MouseDownEvent>(this.MenuNotClicked);
            root.RegisterCallback<MouseDownEvent>(this.MenuNotClicked, TrickleDown.TrickleDown);
        }

        private void MenuNotClicked(MouseDownEvent evt)
        {
            if (this.Menu.ContainsPoint(this.Menu.WorldToLocal(evt.mousePosition)) ||
                this.visualInput.ContainsPoint(this.visualInput.WorldToLocal(evt.mousePosition)))
            {
                return;
            }

            this.HideMenu();
        }

        private VisualElement GetRoot()
        {
            VisualElement p = this;

            do
            {
                if (p.parent.ClassListContains("unity-ui-document__root"))
                {
                    return p.parent;
                }

                p = p.parent;
            }
            while (p.parent != null);

            return p;
        }
    }
}
