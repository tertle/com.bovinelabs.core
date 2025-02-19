# Input
## Summary
Input provides various input related tools
- The [InputAction] attribute that uses source generation to create systems that automatically write InputActions values to component fields every frame
- `InputCommon` is a struct with a lot of built it common data such as Ray from mouse, screen bounds, cursor position etc.
- InputAPI provides burst friend utility for enabling and disabling action maps

## Defining
Define your binding by creating an `ICompnentData` with your desired input. Remember to include partial so the source generator works.
```cs
public partial struct InputTest : IComponentData
{
    [InputAction]
    public float SingleAxis;

    [InputAction]
    public float2 Axis; // Mouse

    [InputActionDelta] // Modified by delta time, useful for controller input
    public float2 AxisDelta; 

    [InputAction]
    public bool Button; // Pressed

    [InputAction]
    public ButtonState ButtonEvents; // Down, Pressed, Up events
}
```
If you have correctly set up your component a system and binding components will be source generated for you. 
Currently, there are no errors for badly setup components, but the 2 most common issues if source generators aren't running are:
- Missing partial
- Assembly missing a reference to Unity.InputSystem

## Assigning Input
Open up Input Common settings via `BovineLabs -> Settings -> Core -> Input Common`.

Click on `Find all Input` to find any newly created components, it should appear under the Settings group now.

Bind each field with the appropriate Input Action.

![InputCommon](Images/InputCommon.png)

## InputCommon

`InputCommon` is a struct updated every frame with common utility used for picking from cursors, interacting with game view or UI.

```csharp
    public struct InputCommon : IComponentData
    {
        /// <summary> The size of the screen. <see cref="Screen.width" /> and <see cref="Screen.height" />. </summary>
        public int2 ScreenSize;

        /// <summary> Point in screen space. Screenspace is defined in pixels. The bottom-left is (0,0); the right-top is (pixelWidth,pixelHeight). </summary>
        public float2 CursorScreenPoint;

        /// <summary> Cursor point in viewport space. Viewport space is normalized. The bottom-left is (0,0); the top-right is (1,1). </summary>
        public float2 CursorViewPoint;

        /// <summary> Cursor point in camera viewport space. Viewport space is normalized. The bottom-left is (0,0); the top-right is (1,1). </summary>
        public float2 CursorCameraViewPoint;

        /// <summary> Is the cursor currently inside the view port. </summary>
        public bool CursorInViewPort;

        /// <summary> Is the cursor currently inside the cameras view port. </summary>
        public bool CursorInCameraViewPort;

        /// <summary> Gets a value indicating whether the cursor is currently over the UI. </summary>
        public bool InputOverUI;

        /// <summary> Gets a value indicating whether the application has focus. </summary>
        public bool ApplicationFocus;

        /// <summary> A ray going from camera through the current <see cref="CursorScreenPoint" /> using <see cref="Camera.ScreenPointToRay(Vector3)" />. </summary>
        /// <remarks> Displacement is set as a unit vector. </remarks>
        public Ray CameraRay;

        /// <summary> Gets a value indicating whether any button was pressed. </summary>
        public bool AnyButtonPress;

        /// <summary>
        /// Gets a value indicating whether the cursor is in the view and the application has focus.
        /// combination of <see cref="CursorInViewPort" /> && <see cref="ApplicationFocus" />.
        /// </summary>
        public bool InViewWithFocus => this.CursorInViewPort && this.ApplicationFocus;
    }
```

To get a lot of this functionality to work, you need to add an `InputAction` to the `CursorPosition` that feeds in Mouse Position.

## InputAPI

`InputAPI` lets you toggle ActionMaps enabled or disabled by using the static methods `InputAPI.InputEnable|InputDisable`. 

For this to work you need to set upp an InputActionAsset in the Asset field for `Input Common Settings`.
You'll also generally want to make some to be enabled on startup by default and that's what the Default Enabled drop down is for.

## References

### Example of the InputTest Source Generator

For reference, this is what the generated source code for your InputTest would look like

```csharp
namespace BovineLabs.Test
{
    using System;
    using BovineLabs.Core.Input;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;
    using UnityEngine;
    using UnityEngine.InputSystem;

    partial struct InputTest
    {
        private struct ActionsGenerated : IComponentData
        {
            public UnityObjectRef<InputActionReference> SingleAxis;
            public UnityObjectRef<InputActionReference> Axis;
            public UnityObjectRef<InputActionReference> AxisDelta;
            public UnityObjectRef<InputActionReference> Button;
            public UnityObjectRef<InputActionReference> ButtonEvents;
        }

        [UpdateInGroup(typeof(InputSystemGroup))]
        private partial class System : SystemBase
        {
            private EntityQuery query;
            private EntityQuery queryActions;
            private InputTest input;
            private float deltaTime;

            protected override void OnCreate()
            {
                this.query = new EntityQueryBuilder(Allocator.Temp).WithAllRW<InputTest>().Build(this);
                this.queryActions = new EntityQueryBuilder(Allocator.Temp).WithAll<ActionsGenerated>().Build(this);

                this.RequireForUpdate(this.query);
            }

            protected override void OnUpdate()
            {
                this.deltaTime = this.World.Time.DeltaTime;
                this.query.CompleteDependency();
                this.query.SetSingleton(this.input);
                this.input.ButtonEvents.Reset();
            }

            protected override void OnStartRunning()
            {
                if (!this.queryActions.TryGetSingleton<ActionsGenerated>(out var actions))
                {
                    Debug.LogError("ActionsGenerated has not been created. Make sure you update InputCommonSettings.");
                    return;
                }

                if (actions.SingleAxis.Value != null)
                {
                    actions.SingleAxis.Value.action.performed += this.OnSingleAxisPerformed;
                    actions.SingleAxis.Value.action.canceled += this.OnSingleAxisCanceled;
                }
                else
                {
                    Debug.LogWarning("InputActionReference for InputTest.SingleAxis has not been assigned.");
                }

                if (actions.Axis.Value != null)
                {
                    actions.Axis.Value.action.performed += this.OnAxisPerformed;
                    actions.Axis.Value.action.canceled += this.OnAxisCanceled;
                }
                else
                {
                    Debug.LogWarning("InputActionReference for InputTest.Axis has not been assigned.");
                }

                if (actions.AxisDelta.Value != null)
                {
                    actions.AxisDelta.Value.action.performed += this.OnAxisDeltaPerformed;
                    actions.AxisDelta.Value.action.canceled += this.OnAxisDeltaCanceled;
                }
                else
                {
                    Debug.LogWarning("InputActionReference for InputTest.AxisDelta has not been assigned.");
                }

                if (actions.Button.Value != null)
                {
                    actions.Button.Value.action.started += this.OnButtonStarted;
                    actions.Button.Value.action.canceled += this.OnButtonCanceled;
                }
                else
                {
                    Debug.LogWarning("InputActionReference for InputTest.Button has not been assigned.");
                }

                if (actions.ButtonEvents.Value != null)
                {
                    actions.ButtonEvents.Value.action.started += this.OnButtonEventsStarted;
                    actions.ButtonEvents.Value.action.canceled += this.OnButtonEventsCanceled;
                }
                else
                {
                    Debug.LogWarning("InputActionReference for InputTest.ButtonEvents has not been assigned.");
                }
            }

            protected override void OnStopRunning()
            {
                if (this.queryActions.TryGetSingleton<ActionsGenerated>(out var actions))
                {
                    if (actions.SingleAxis.Value != null)
                    {
                        actions.SingleAxis.Value.action.performed -= this.OnSingleAxisPerformed;
                        actions.SingleAxis.Value.action.canceled -= this.OnSingleAxisCanceled;
                    }
                    if (actions.Axis.Value != null)
                    {
                        actions.Axis.Value.action.performed -= this.OnAxisPerformed;
                        actions.Axis.Value.action.canceled -= this.OnAxisCanceled;
                    }
                    if (actions.AxisDelta.Value != null)
                    {
                        actions.AxisDelta.Value.action.performed -= this.OnAxisDeltaPerformed;
                        actions.AxisDelta.Value.action.canceled -= this.OnAxisDeltaCanceled;
                    }
                    if (actions.Button.Value != null)
                    {
                        actions.Button.Value.action.started -= this.OnButtonStarted;
                        actions.Button.Value.action.canceled -= this.OnButtonCanceled;
                    }
                    if (actions.ButtonEvents.Value != null)
                    {
                        actions.ButtonEvents.Value.action.started -= this.OnButtonEventsStarted;
                        actions.ButtonEvents.Value.action.canceled -= this.OnButtonEventsCanceled;
                    }
                }
            }

            private void OnSingleAxisPerformed(InputAction.CallbackContext context)
            {
                this.input.SingleAxis = (float)context.ReadValue<float>();
            }

            private void OnSingleAxisCanceled(InputAction.CallbackContext context)
            {
                this.input.SingleAxis = default;
            }

            private void OnAxisPerformed(InputAction.CallbackContext context)
            {
                this.input.Axis = (float2)context.ReadValue<Vector2>();
            }

            private void OnAxisCanceled(InputAction.CallbackContext context)
            {
                this.input.Axis = default;
            }

            private void OnAxisDeltaPerformed(InputAction.CallbackContext context)
            {
                this.input.AxisDelta = (float2)context.ReadValue<Vector2>() * this.deltaTime;
            }

            private void OnAxisDeltaCanceled(InputAction.CallbackContext context)
            {
                this.input.AxisDelta = default;
            }

            private void OnButtonStarted(InputAction.CallbackContext context)
            {
                this.input.Button = true;
            }

            private void OnButtonCanceled(InputAction.CallbackContext context)
            {
                this.input.Button = false;
            }

            private void OnButtonEventsStarted(InputAction.CallbackContext context)
            {
                this.input.ButtonEvents.Started();
            }

            private void OnButtonEventsCanceled(InputAction.CallbackContext context)
            {
                this.input.ButtonEvents.Cancelled();
            }
        }

        [Serializable]
        private class Settings : IInputSettings
        {
            [HideInInspector]
            public string Name = "InputTest";
            public InputActionReference SingleAxis;
            public InputActionReference Axis;
            public InputActionReference AxisDelta;
            public InputActionReference Button;
            public InputActionReference ButtonEvents;
            public void Bake(IBakerWrapper baker)
            {
                baker.AddComponent(new ActionsGenerated
                {
                    SingleAxis = this.SingleAxis,
                    Axis = this.Axis,
                    AxisDelta = this.AxisDelta,
                    Button = this.Button,
                    ButtonEvents = this.ButtonEvents,
                });
                baker.AddComponent(default(InputTest));
            }
        }
    }
}
```