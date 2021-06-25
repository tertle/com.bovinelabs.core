// <copyright file="MouseEvents.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Basics.UI
{
    using System;
    using UnityEngine.UIElements;

    /// <summary> A manipulator that fires events from mouse buttons. </summary>
    public class MouseEvents : MouseManipulator
    {
        // In milliseconds
        private readonly long clickLimit;
        private bool active;

        private bool delayElapsed;
        private IVisualElementScheduledItem scheduler;

        /// <summary> Initializes a new instance of the <see cref="MouseEvents" /> class. </summary>
        /// <param name="clickLimit"> The time limit a click will register. Disable with a value less than or equal to 0. </param>
        public MouseEvents(long clickLimit = 250L)
        {
            this.clickLimit = clickLimit;

            this.activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
            this.activators.Add(new ManipulatorActivationFilter { button = MouseButton.RightMouse });
            this.activators.Add(new ManipulatorActivationFilter { button = MouseButton.MiddleMouse });
        }

        /// <summary> Gets mouse events for any button. </summary>
        public Events Any { get; } = new Events();

        /// <summary> Gets mouse events for the left button. </summary>
        public Events Left { get; } = new Events();

        /// <summary> Gets mouse events for the right button. </summary>
        public Events Right { get; } = new Events();

        /// <summary> Gets mouse events for the middle button. </summary>
        public Events Middle { get; } = new Events();

        /// <inheritdoc />
        protected override void RegisterCallbacksOnTarget()
        {
            this.target.RegisterCallback<MouseDownEvent>(this.OnMouseDown);
            this.target.RegisterCallback<MouseMoveEvent>(this.OnMouseMove);
            this.target.RegisterCallback<MouseUpEvent>(this.OnMouseUp);
        }

        /// <inheritdoc />
        protected override void UnregisterCallbacksFromTarget()
        {
            this.target.UnregisterCallback<MouseDownEvent>(this.OnMouseDown);
            this.target.UnregisterCallback<MouseMoveEvent>(this.OnMouseMove);
            this.target.UnregisterCallback<MouseUpEvent>(this.OnMouseUp);
        }

        private void OnMouseDown(MouseDownEvent evt)
        {
            if (!this.CanStartManipulation(evt))
            {
                return;
            }

            this.active = true;

            this.target.CapturePointer(PointerId.mousePointerId);

            this.delayElapsed = false;

            if (this.clickLimit > 0)
            {
                if (this.scheduler == null)
                {
                    this.scheduler = this.target.schedule.Execute(() => this.delayElapsed = true).StartingIn(this.clickLimit);
                }
                else
                {
                    this.scheduler.ExecuteLater(this.clickLimit);
                }
            }

            this.Any.OnDown(evt);
            switch (evt.button)
            {
                case 0:
                    this.Left.OnDown(evt);
                    break;
                case 1:
                    this.Right.OnDown(evt);
                    break;
                case 2:
                    this.Middle.OnDown(evt);
                    break;
            }

            evt.StopImmediatePropagation();
        }

        private void OnMouseMove(MouseMoveEvent evt)
        {
            this.Any.OnMove(evt);
            switch (evt.button)
            {
                case 0:
                    this.Left.OnMove(evt);
                    break;
                case 1:
                    this.Right.OnMove(evt);
                    break;
                case 2:
                    this.Middle.OnMove(evt);
                    break;
            }

            if (!this.active)
            {
                return;
            }

            this.Any.OnDragged(evt);
            switch (evt.button)
            {
                case 0:
                    this.Left.OnDragged(evt);
                    break;
                case 1:
                    this.Right.OnDragged(evt);
                    break;
                case 2:
                    this.Middle.OnDragged(evt);
                    break;
            }

            evt.StopPropagation();
        }

        private void OnMouseUp(MouseUpEvent evt)
        {
            if (!this.active || !this.CanStopManipulation(evt))
            {
                return;
            }

            this.active = false;

            this.target.ReleasePointer(PointerId.mousePointerId);

            if (this.clickLimit > 0)
            {
                // Repeatable button clicks are performed on the MouseDown and at timer events only
                this.scheduler?.Pause();
            }

            if (!this.delayElapsed && this.target.ContainsPoint(evt.localMousePosition))
            {
                this.Any.OnClicked(evt);
                switch (evt.button)
                {
                    case 0:
                        this.Left.OnClicked(evt);
                        break;
                    case 1:
                        this.Right.OnClicked(evt);
                        break;
                    case 2:
                        this.Middle.OnClicked(evt);
                        break;
                }
            }

            this.Any.OnUp(evt);
            switch (evt.button)
            {
                case 0:
                    this.Left.OnUp(evt);
                    break;
                case 1:
                    this.Right.OnUp(evt);
                    break;
                case 2:
                    this.Middle.OnUp(evt);
                    break;
            }

            evt.StopPropagation();
        }

        /// <summary> Collection of events for a specific button. </summary>
        public class Events
        {
            /// <summary> Fired when the mouse clicked. </summary>
            public event Action<MouseUpEvent> Clicked;

            /// <summary> Fired when the mouse button is pressed. </summary>
            public event Action<MouseDownEvent> Down;

            /// <summary> Fired when the mouse button is dragged and moves. </summary>
            public event Action<MouseMoveEvent> Dragged;

            /// <summary> Fired when the mouse button is moved. </summary>
            public event Action<MouseMoveEvent> Move;

            /// <summary> Fired when the mouse is released. </summary>
            public event Action<MouseUpEvent> Up;

            internal void OnClicked(MouseUpEvent evt)
            {
                this.Clicked?.Invoke(evt);
            }

            internal void OnDown(MouseDownEvent evt)
            {
                this.Down?.Invoke(evt);
            }

            internal void OnDragged(MouseMoveEvent evt)
            {
                this.Dragged?.Invoke(evt);
            }

            internal void OnMove(MouseMoveEvent evt)
            {
                this.Move?.Invoke(evt);
            }

            internal void OnUp(MouseUpEvent evt)
            {
                this.Up?.Invoke(evt);
            }
        }
    }
}