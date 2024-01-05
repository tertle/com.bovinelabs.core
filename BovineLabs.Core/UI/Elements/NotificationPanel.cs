// <copyright file="NotificationPanel.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.UI
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using Unity.Properties;
    using UnityEngine;
    using UnityEngine.UIElements;

    [UxmlElement]
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Required for binding")]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:Element should begin with upper-case letter", Justification = "Required for binding")]
    public partial class NotificationPanel : BindableElement
    {
        public const string USSClassName = "bl-notification";
        public const string BackgroundUSSClassName = USSClassName + "__background";
        public const string PanelUSSClassName = USSClassName + "__panel";
        public const string LabelUSSClassName = USSClassName + "__label";
        public const string ButtonUssClassName = USSClassName + "__button";
        public const string ButtonAcceptUssClassName = ButtonUssClassName + "--accept";
        public const string ButtonCancelUssClassName = ButtonUssClassName + "--cancel";
        public const string ButtonGroupUssClassName = USSClassName + "bl-notification__button--group";

        private readonly VisualElement background;
        private readonly Label labelElement;
        private readonly Button accept;
        private readonly Button cancel;

        private ButtonEvent acceptedAction;
        private ButtonEvent cancelAction;

        private bool acceptedFromButton;
        private bool cancelledFromButton;

        public NotificationPanel()
        {
            this.AddToClassList(USSClassName);
            this.ApplyDefaultStyle();

            this.background = SetupBackground();

            var panelBackground = SetupPanelBackground();
            this.background.Add(panelBackground);

            this.labelElement = SetupLabel();
            panelBackground.Add(this.labelElement);

            var buttonGroup = SetupButtonGroup();
            panelBackground.Add(buttonGroup);

            this.accept = SetupButton("Accept");
            this.accept.AddToClassList(ButtonAcceptUssClassName);
            this.accept.clicked += () =>
            {
                this.acceptedFromButton = true;
                this.accepted = true;
            };
            buttonGroup.Add(this.accept);

            this.cancel = SetupButton("Cancel");
            this.cancel.AddToClassList(ButtonCancelUssClassName);
            this.cancel.clicked += () =>
            {
                this.cancelledFromButton = true;
                this.cancelled = true;
            };
            buttonGroup.Add(this.cancel);
        }

        public event Action Accepted
        {
            add => this.accept.clicked += value;
            remove => this.accept.clicked -= value;
        }

        public event Action Cancelled
        {
            add => this.cancel.clicked += value;
            remove => this.cancel.clicked -= value;
        }

        [CreateProperty]
        [UxmlAttribute("accepted")]
        public bool accepted
        {
            get => this.acceptedAction.TryConsume();
            set
            {
                if (this.acceptedFromButton && this.acceptedAction.TryProduce(value))
                {
                    this.Notify();
                    this.acceptedFromButton = false;
                }
            }
        }

        [CreateProperty]
        [UxmlAttribute("cancelled")]
        public bool cancelled
        {
            get => this.cancelAction.TryConsume();
            set
            {
                if (this.cancelledFromButton && this.cancelAction.TryProduce(value))
                {
                    this.Notify();
                    this.cancelledFromButton = false;
                }
            }
        }

        [CreateProperty]
        [UxmlAttribute("notification-visible")]
        public bool notificationVisible
        {
            get => this.background.parent != null;
            set
            {
                if (this.notificationVisible == value)
                {
                    return;
                }

                if (value)
                {
                    this.Add(this.background);
                }
                else
                {
                    this.background.RemoveFromHierarchy();
                }

                this.Notify();
            }
        }

        [CreateProperty]
        [UxmlAttribute("label")]
        public string? label
        {
            get => this.labelElement.text;
            set
            {
                if (this.labelElement.text == value)
                {
                    return;
                }

                this.labelElement.text = value;
                this.Notify();
            }
        }

        [CreateProperty]
        [UxmlAttribute("accept-label")]
        public string? acceptLabel
        {
            get => this.accept.text;
            set
            {
                if (this.accept.text == value)
                {
                    return;
                }

                this.accept.text = value;
                this.Notify();
            }
        }

        [CreateProperty]
        [UxmlAttribute("cancel-label")]
        public string? cancelLabel
        {
            get => this.cancel.text;
            set
            {
                if (this.cancel.text == value)
                {
                    return;
                }

                this.cancel.text = value;
                this.Notify();
            }
        }

        private void ApplyDefaultStyle()
        {
            this.pickingMode = PickingMode.Ignore;
            this.style.position = Position.Absolute;
            this.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
            this.style.height = new StyleLength(new Length(100, LengthUnit.Percent));
        }

        private static VisualElement SetupBackground()
        {
            var ve = new VisualElement
            {
                style =
                {
                    position = Position.Relative,
                    // width = new StyleLength(new Length(100, LengthUnit.Percent)),
                    // height = new StyleLength(new Length(100, LengthUnit.Percent)),
                    flexGrow = 1,
                    alignItems = Align.Center,
                    justifyContent = Justify.Center,
                    backgroundColor = new Color(0, 0, 0, 0.5f),
                },
            };
            ve.AddToClassList(BackgroundUSSClassName);
            return ve;
        }

        private static VisualElement SetupPanelBackground()
        {
            var ve = new VisualElement
            {
                style =
                {
                    width = 300,
                    height = 200,
                    paddingBottom = 8,
                    paddingTop = 8,
                    paddingLeft = 8,
                    paddingRight = 8,
                    backgroundColor = new Color(0.2196f, 0.2196f, 0.2196f),
                    borderBottomColor = Color.black,
                    borderTopColor = Color.black,
                    borderLeftColor = Color.black,
                    borderRightColor = Color.black,
                    borderBottomWidth = 1,
                    borderTopWidth = 1,
                    borderLeftWidth = 1,
                    borderRightWidth = 1,
                    // justifyContent = Justify.SpaceBetween,
                },
            };
            ve.AddToClassList(PanelUSSClassName);
            ve.AddToClassList(USS.PanelUss);
            return ve;
        }

        private static Label SetupLabel()
        {
            var label = new Label();
            label.AddToClassList(LabelUSSClassName);
            label.text = "Some notification text";
            label.style.color = new Color(0.7529f, 0.7529f, 0.7529f);
            label.style.flexGrow = 1;
            return label;
        }

        private static VisualElement SetupButtonGroup()
        {
            var button = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    justifyContent = Justify.SpaceEvenly,
                },
            };
            button.AddToClassList(ButtonGroupUssClassName);
            return button;
        }

        private static Button SetupButton(string text)
        {
            var button = new Button
            {
                text = text,
            };
            button.AddToClassList(ButtonUssClassName);
            return button;
        }
    }
}
