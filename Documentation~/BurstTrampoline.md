# BurstTrampoline

## Summary

BurstTrampoline provides a lightweight bridge from Burst-compiled code to managed delegates. It keeps a static invoker alive and pins the managed target with a `GCHandle`, letting you jump out of Burst easily.

**Highlights:**
- Works with zero to four input parameters through `BurstTrampoline`, `BurstTrampoline<T>`, `BurstTrampoline<T1, T2>`, `BurstTrampoline<T1, T2, T3>`, and `BurstTrampoline<T1, T2, T3, T4>`
- `BurstTrampolineOut` variants let managed code return data back to Burst callers

## Example

The following example syncs an AudioSource volume to a component

```csharp
public struct AudioSourceData : IComponentData
{
    public float Volume;
}

public struct AudioFacade : IComponentData
{
    public UnityObjectRef<AudioSource> AudioSource;
}

[BurstCompile]
public partial struct AudioSyncSystem : ISystem
{
    static AudioSyncSystem()
    {
        Burst.AudioSource.Data = new BurstTrampoline<AudioFacade, AudioSourceData>(AudioSourceChanged);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var sources = SystemAPI.GetSingleton<AudioSourcePool>().AudioSources;
    
        foreach (var (facade, component) in SystemAPI.Query<AudioFacade, AudioSourceData>())
        {
            Burst.AudioSource.Data.Invoke(facade, component);
        }
    }
    
    [MonoPInvokeCallback(typeof(BurstTrampoline<AudioFacade, AudioSourceData>.Delegate))]
    private static void AudioSourceChanged(in AudioFacade facade, in AudioSourceData component)
    {
        facade.AudioSource.Value.volume = component.Volume;
    }
    
    private static class Burst
    {
        public static readonly SharedStatic<BurstTrampoline<AudioFacade, AudioSourceData>> AudioSource =
            SharedStatic<BurstTrampoline<AudioFacade, AudioSourceData>>.GetOrCreate<AudioSyncSystem>();
    }
}
```
