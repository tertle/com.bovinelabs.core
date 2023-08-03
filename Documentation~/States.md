# State
## Summary
Provides states on entities by mapping a bit field to components automatically, adding and removing them without every state needing to know about every other state's components. This is particularly useful for high-level game application states or for UI.

## Model
There are 5 models currently available:

|Model|Description|
|----|----|
|StateModel|Only allows one state to be active at a time.|
|StateFlagModel|Flag-based states allowing multiple to be active at a time.|
|StateModelWithHistory|Same as StateModel but also stores forward and back history.|
|StateFlagModelWithHistory|Same as StateFlagModel but also stores forward and back history.|
|StateModelEnableable|Same as StateModel but instead uses IEnableable for states.|

### StateAPI
Provides a convenient way for a system to register a state. It also has an overload that supports [K](K.md).
By default, `StateAPI.Register` will add a `RequireForUpdate` on the component to the system. You can override this behavior by setting the ``queryDependency` parameter to false.

## Example
### StateModel
Setup current and previous state components
```cs
    public struct CameraState : IComponentData
    {
        public byte Value;
    }
    
    internal struct CameraStatePrevious : IComponentData
    {
        public byte Value;
    }
```

Create a state system to setup the StateModel
```cs
    public partial struct CameraStateSystem : ISystem, ISystemStartStop
    {
        private StateModel impl;

        [BurstCompile]
        public void OnStartRunning(ref SystemState state)
        {
            this.impl = new StateModel(ref state, ComponentType.ReadWrite<CameraState>(), ComponentType.ReadWrite<CameraStatePrevious>());
        }

        [BurstCompile]
        public void OnStopRunning(ref SystemState state)
        {
            this.impl.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator); // This can't be CommandBuffer System if prefered
            this.impl.Run(ref state, ecb);
            ecb.Playback(state.EntityManager);
        }
    }
```

Register your states using StateAPI in systems that own them
```cs
    private struct FreeCameraState : IComponentData
    {
    }
    
    public enum CameraStates : byte
    {
        None = 0,

        TopDown = 250,
        ThirdPerson = 251,
        Follow = 252,
        Pan = 253,
        Free = 254,
    }

    [UpdateInGroup(typeof(ClientStateSystemGroup))]
    public partial struct FreeCameraSystem : ISystem, ISystemStartStop
    {
        /// <inheritdoc/>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            StateAPI.Register<CameraState, FreeCameraState>(ref state, (byte)CameraStates.Free);
        }
```

Toggle a state
```csharp
    // Switch to state 15
    int state = 15;
    SystemAPI.SetSingleton(new CameraState { Value = state });
```

### StateFlagModel
Setup current and previous state components. This can be any size up to 256. I like to use my [BitArray](../Collections/BitArray.cs) for convenience.
```cs
    public struct ClientState : IComponentData
    {
        public BitArray256 Value;
    }

    public struct ClientStatePrevious : IComponentData
    {
        public BitArray256 Value;
    }
```

Create a state system to setup the StateFlagModel
```cs
    public partial struct ClientStateSystem : ISystem, ISystemStartStop
    {
        private StateFlagModel impl;

        [BurstCompile]
        public void OnStartRunning(ref SystemState state)
        {
            this.impl = new StateFlagModel(ref state, ComponentType.ReadWrite<ClientState>(), ComponentType.ReadWrite<ClientStatePrevious>());
        }

        [BurstCompile]
        public void OnStopRunning(ref SystemState state)
        {
            this.impl.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
            this.impl.Run(ref state, ecb);
            ecb.Playback(state.EntityManager);
        }
    }
```

```cs
    public partial struct OptionsStateSystem : ISystem, ISystemStartStop
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            StateAPI.Register<ClientState, StateOptions, ClientStates>(ref state, "options");
        }
```
This example is using the other overload to benefit from K.

```csharp
    // Switch to state 4
    int state = 4;
    SystemAPI.SetSingleton(new ClientState { Value = new BitArray256 { [state] = true } });
    
    // Enable state 3 but keeping existing states
    int state = 3;
    var clientState = SystemAPI.GetSingletonRW<UIState>();
    clientState.ValueRW.Value[state] = true;
```

### History
TODO
