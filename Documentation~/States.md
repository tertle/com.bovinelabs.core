# States

## Summary

Provides states on entities by mapping a bit field to components automatically, adding and removing them without every state needing to know about every other state's components. Particularly useful for high-level game application states or UI.

## Available Models

|Model|Description|
|----|----|
|StateModel|Only allows one state to be active at a time.|
|StateFlagModel|Flag-based states allowing multiple to be active at a time.|
|StateModelWithHistory|Same as StateModel but also stores forward and back history.|
|StateFlagModelWithHistory|Same as StateFlagModel but also stores forward and back history.|
|StateModelEnableable|Same as StateModel but instead uses IEnableable for states.|

### StateAPI
Provides a convenient way for a system to register a state. Also has an overload that supports [K](K.md).
By default, `StateAPI.Register` adds a `RequireForUpdate` on the component to the system. Override this behavior by setting the `queryDependency` parameter to false.

## Usage

### StateModel Example
Setup current and previous state components:
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

Create a state system:
```cs
public partial struct CameraStateSystem : ISystem, ISystemStartStop
{
    private StateModel impl;

    public void OnStartRunning(ref SystemState state)
    {
        impl = new StateModel(ref state, ComponentType.ReadWrite<CameraState>(), ComponentType.ReadWrite<CameraStatePrevious>());
    }

    public void OnStopRunning(ref SystemState state)
    {
        impl.Dispose();
    }

    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
        impl.Run(ref state, ecb);
        ecb.Playback(state.EntityManager);
    }
}
```

Register states using StateAPI:
```cs
public enum CameraStates : byte
{
    None = 0,
    TopDown = 250,
    ThirdPerson = 251,
    Follow = 252,
    Free = 254,
}

public partial struct FreeCameraSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        StateAPI.Register<CameraState, FreeCameraState>(ref state, (byte)CameraStates.Free);
    }
}
```

Toggle a state:
```csharp
// Switch to state 15
SystemAPI.SetSingleton(new CameraState { Value = 15 });
```

### StateFlagModel Example
Setup current and previous state components (can be any size up to 256):
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

Create a state system:
```cs
public partial struct ClientStateSystem : ISystem, ISystemStartStop
{
    private StateFlagModel impl;

    public void OnStartRunning(ref SystemState state)
    {
        impl = new StateFlagModel(ref state, ComponentType.ReadWrite<ClientState>(), ComponentType.ReadWrite<ClientStatePrevious>());
    }

    public void OnStopRunning(ref SystemState state)
    {
        impl.Dispose();
    }

    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
        impl.Run(ref state, ecb);
        ecb.Playback(state.EntityManager);
    }
}
```

Register states (this example uses the K overload):
```cs
public partial struct OptionsStateSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        StateAPI.Register<ClientState, StateOptions, ClientStates>(ref state, "options");
    }
}
```

Toggle states:
```csharp
// Switch to state 4
SystemAPI.SetSingleton(new ClientState { Value = new BitArray256 { [4] = true } });

// Enable state 3 but keeping existing states
var clientState = SystemAPI.GetSingletonRW<ClientState>();
clientState.ValueRW.Value[3] = true;
```
