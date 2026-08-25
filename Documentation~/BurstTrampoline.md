# BurstTrampoline

`BurstTrampoline` synchronously dispatches from Burst-compiled code to a managed callback through one unmanaged payload pointer and byte size.

```csharp
using BovineLabs.Core.Utility;
```

Use it for a narrow managed boundary that cannot be expressed in Burst, such as forwarding main-thread ECS state to a managed Unity object. It is not a job scheduler, message queue, or thread hop: the managed callback runs immediately on the thread that calls `Invoke`.

## Core API

Construct a trampoline from a managed callback with this function-pointer signature:

```csharp
delegate*<void*, int, void>
```

The callback receives the payload pointer and its size. The public members are:

```csharp
new BurstTrampoline(&ManagedCallback);
bool IsCreated { get; }
void Invoke(void* argumentsPtr, int argumentsSize);
void Invoke<T>(ref T arguments) where T : unmanaged;
static ref T ArgumentsFromPtr<T>(void* argumentsPtr, int size) where T : unmanaged;
```

`Invoke` throws when the trampoline is not initialized. Payload-size validation in `ArgumentsFromPtr<T>` is present only when collection checks are enabled, so the caller and callback must always agree on the exact type and layout.

## Initialization

Store reusable trampolines in `SharedStatic<BurstTrampoline>` and initialize them from explicit Unity lifecycle callbacks. Do not initialize a trampoline in a static constructor reachable from Burst code.

Use `InitializeOnLoadMethod` in the editor and subsystem registration in players:

```csharp
public static readonly SharedStatic<BurstTrampoline> Callback =
    SharedStatic<BurstTrampoline>.GetOrCreate<MyOwner, CallbackType>();

#if UNITY_EDITOR
[UnityEditor.InitializeOnLoadMethod]
#else
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
#endif
private static unsafe void InitializeTrampolines()
{
    Callback.Data = new BurstTrampoline(&ManagedCallback);
}

private struct CallbackType
{
}
```

This ensures the `SharedStatic` value is restored after subsystem resets before Burst execution can invoke it.

## Packed helper overloads

`BurstTrampolineExtensions` covers common payload layouts:

| Call | Callback payload |
|---|---|
| `Invoke()` | `BurstManagedNoArgs` |
| `Invoke(in value)` | `T` |
| `Invoke(in first, in second)` | `BurstManagedPair<TFirst, TSecond>` |
| `Invoke(in first, in second, in third)` | `BurstManagedTriple<TFirst, TSecond, TThird>` |
| `InvokeRef(ref value)` | `TRef`; callback may mutate it |
| `InvokeOut(out value)` | `TOut`; callback writes the payload itself |
| `InvokeOut(in input, out value)` | Pair; callback writes `Second` |
| `InvokeOut(in first, in second, out value)` | Triple; callback writes `Third` |

For four or more fields or a domain-specific layout, define one unmanaged payload struct and call `Invoke(ref payload)` directly.

## Main-thread managed-object example

This example invokes the managed callback from a system's main-thread `OnUpdate`. It does not call Unity object APIs from a worker job.

```csharp
public struct AudioSourceData : IComponentData
{
    public float Volume;
    public float Pitch;
}

public struct AudioFacade : IComponentData
{
    public UnityObjectRef<AudioSource> AudioSource;
}

[BurstCompile]
public unsafe partial struct AudioSyncSystem : ISystem
{
    private static readonly SharedStatic<BurstTrampoline> AudioSourceChanged =
        SharedStatic<BurstTrampoline>.GetOrCreate<AudioSyncSystem, AudioSourceChangedType>();

#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
#else
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
#endif
    private static void InitializeTrampolines()
    {
        AudioSourceChanged.Data = new BurstTrampoline(&AudioSourceChangedPacked);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (facade, data) in SystemAPI.Query<RefRO<AudioFacade>, RefRO<AudioSourceData>>())
        {
            AudioSourceChanged.Data.Invoke(facade.ValueRO, data.ValueRO);
        }
    }

    private static void AudioSourceChangedPacked(void* argumentsPtr, int argumentsSize)
    {
        ref var arguments = ref BurstTrampoline.ArgumentsFromPtr<BurstManagedPair<AudioFacade, AudioSourceData>>(
            argumentsPtr,
            argumentsSize);

        var audioSource = arguments.First.AudioSource.Value;
        if (!audioSource)
        {
            return;
        }

        audioSource.volume = arguments.Second.Volume;
        audioSource.pitch = arguments.Second.Pitch;
    }

    private struct AudioSourceChangedType
    {
    }
}
```

The user callback does not need `MonoPInvokeCallback`; Core applies that attribute to its internal wrapper delegate.

## Returning data

For one input and one output, unpack the same pair and assign `Second`:

```csharp
public unsafe struct Readback
{
    private static readonly SharedStatic<BurstTrampoline> Callback =
        SharedStatic<BurstTrampoline>.GetOrCreate<Readback, CallbackType>();

    public struct Request
    {
        public int Value;
    }

    public struct Result
    {
        public bool IsPositive;
    }

#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
#else
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
#endif
    private static void InitializeTrampoline()
    {
        Callback.Data = new BurstTrampoline(&ReadbackPacked);
    }

    [BurstCompile]
    public static Result Evaluate(in Request request)
    {
        Callback.Data.InvokeOut(in request, out Result result);
        return result;
    }

    private static void ReadbackPacked(void* argumentsPtr, int argumentsSize)
    {
        ref var arguments = ref BurstTrampoline.ArgumentsFromPtr<BurstManagedPair<Request, Result>>(
            argumentsPtr,
            argumentsSize);

        arguments.Second = new Result { IsPositive = arguments.First.Value > 0 };
    }

    private struct CallbackType
    {
    }
}
```

Use `InvokeRef(ref value)` when the callback should mutate one existing unmanaged payload rather than produce a separately named output.

## Safety and limitations

- Every payload type must be unmanaged.
- The callback is synchronous. Never retain `argumentsPtr` or a ref returned by `ArgumentsFromPtr<T>` after the callback returns.
- A callback invoked by a worker job runs on that worker. Most `UnityEngine.Object` APIs are main-thread-only, so keep those callbacks on a main-thread Burst call site or marshal the work through an appropriate queue.
- Do not allow exceptions to cross the unmanaged callback boundary.
- Keep callbacks small. Repeated transitions to managed code can dominate otherwise inexpensive Burst work.
- Initialize every trampoline before any possible call and use `IsCreated` only when absence is a valid state.

## Related guides

- [Utility](Utility.md) summarizes other Burst and runtime helpers.
- [Jobs](Jobs.md) covers scheduled work; a trampoline itself does not schedule or synchronize jobs.
