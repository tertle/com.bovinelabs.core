# BurstTrampoline

## Summary

`BurstTrampoline` provides a lightweight bridge from Burst-compiled code to managed code through a single payload pointer and payload size.

**Highlights:**
- One `BurstTrampoline` type for every signature
- Raw payload API for full control over argument layout
- Extension helpers for zero to three inputs and common `out` patterns
- `ArgumentsFromPtr<T>()` for safe payload unpacking on the managed side
- No user-facing `MonoPInvokeCallback` or delegate boilerplate
- Explicit lifecycle initialization instead of Burst-reachable static constructors

## Core API

`BurstTrampoline` is constructed from a managed callback with the signature:

```csharp
delegate*<void*, int, void>
```

The callback receives:
- `argumentsPtr`: pointer to an unmanaged payload
- `argumentsSize`: payload size in bytes

Core members:
- `new BurstTrampoline(&ManagedCallback)`
- `Invoke(void* argumentsPtr, int argumentsSize)`
- `Invoke<T>(ref T arguments)`
- `ArgumentsFromPtr<T>(void* argumentsPtr, int size)`

## Initialization

Store trampolines in `SharedStatic<BurstTrampoline>` fields and assign them from an explicit Unity lifecycle callback after Burst shared statics have been reset and before Burst execution can touch them.
Do not construct `new BurstTrampoline(&ManagedCallback)` in a static constructor that can be reached from Burst-compiled code.

Use `InitializeOnLoadMethod` in the editor and `RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)` in players.

## Helper payload types

For common cases, `BurstTrampolineExtensions` packs arguments for you:

- `Invoke()` for no arguments
- `Invoke<T>(in T value)` for one argument
- `Invoke<TFirst, TSecond>(in TFirst first, in TSecond second)` for two arguments
- `Invoke<TFirst, TSecond, TThird>(in TFirst first, in TSecond second, in TThird third)` for three arguments
- `InvokeOut<TOut>(out TOut value)` and overloads for simple readback patterns

The helpers use these payload structs:
- `BurstManagedNoArgs`
- `BurstManagedPair<TFirst, TSecond>`
- `BurstManagedTriple<TFirst, TSecond, TThird>`

If you need four or more values, or a custom layout, define your own unmanaged payload struct and call `Invoke(ref payload)` directly.

## Example

The following example matches the packed callback pattern used by Bridge `AudioSyncSystem`:

```csharp
using BovineLabs.Core.Utility;
using Unity.Burst;
using Unity.Entities;
using UnityEngine;

public struct AudioSourceData : IComponentData
{
    public float Volume;
}

public struct AudioFacade : IComponentData
{
    public UnityObjectRef<AudioSource> AudioSource;
}

[BurstCompile]
public unsafe partial struct AudioSyncSystem : ISystem
{
    public static readonly SharedStatic<BurstTrampoline> AudioSource = SharedStatic<BurstTrampoline>.GetOrCreate<AudioSyncSystem>();

#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
#else
    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
#endif
    private static void InitializeTrampolines()
    {
        AudioSource.Data = new BurstTrampoline(&AudioSourceChangedPacked);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (facade, component) in SystemAPI.Query<RefRO<AudioFacade>, RefRO<AudioSourceData>>())
        {
            AudioSource.Data.Invoke(facade.ValueRO, component.ValueRO);
        }
    }

    private static void AudioSourceChangedPacked(void* argumentsPtr, int argumentsSize)
    {
        ref var arguments = ref BurstTrampoline.ArgumentsFromPtr<BurstManagedPair<AudioFacade, AudioSourceData>>(argumentsPtr, argumentsSize);
        ref var facade = ref arguments.First;
        ref var component = ref arguments.Second;
        var audioSource = facade.AudioSource.Value;
        audioSource.volume = component.Volume;
        audioSource.pitch = component.Pitch;
    }
}
```

`MonoPInvokeCallback` is only used inside `BurstTrampoline` itself for its internal wrapper delegate. User callbacks passed to `new BurstTrampoline(&MyPackedCallback)` do not need that attribute.

## Returning data to Burst callers

Use the `InvokeOut` helpers when the managed callback needs to write a result back into the payload:

```csharp
Burst.Readback.Data.InvokeOut(in request, out result);
```

On the managed side, unpack the matching payload and write the output field before returning.
