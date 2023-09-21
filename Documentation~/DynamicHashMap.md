# DynamicHashMap
## Summary
Adds HashMap support to entities by reinterpreting a DynamicBuffer. 

## Setup

Use the following interfaces depending on the type of container you want:

| Type           | Interface                            |
|----------------|--------------------------------------|
| HashMap        | IDynamicHashMap<TKey, TValue>        |
| MultiHashMap   | IDynamicMultiHashMap<TKey, TValue>   |
| HashSet        | IDynamicHashSet<TKey, TValue>        |
| PerfectHashMap | IDynamicPerfectHashMap<TKey, TValue> | 

The following examples use IDynamicHashMap, but IDynamicMultiHashMap and IDynamicHashSet work in the same way.

For example:

```csharp
[InternalBufferCapacity(0)]
public struct MyHashMap : IDynamicHashMap<byte, int>
{
     byte IDynamicHashMap<byte, int>.Value { get; }
}
```

Note that the value field ensures the buffer is the right size; however, it is not directly used.

## Initialization
HashMaps must be initialized before use; this is usually done in your baker.
```cs
baker.AddBuffer<MyHashMap>().InitializeHashMap<MyHashMap, byte, int>();
```

## Using
To use the HashMap, query the underlying DynamicBuffer and reinterpret it to the hashmap.

```cs
[BurstCompile]
public partial struct MyJob : IJobEntity
{
    public void Execute(DynamicBuffer<MyHashMap> buffer)
    {
        var hashMap = buffer.AsHashMap<MyHashMap, byte, int>();       
    }
}
```

From here, you can use it as a normal hashmap.

## Tips
Having to remember to do the full generic invoke, `AsHashMap<MyHashMap, byte, int>()`, can get a bit tedious.
I recommend writing a small extension to simplify this.

```cs
public static class MyHashMapExtensions
{
    public static DynamicBuffer<MyHashMap> Initialize(this DynamicBuffer<MyHashMap> buffer)
    {
        return buffer.InitializeHashMap<MyHashMap, byte, int>();
    }

    public static DynamicHashMap<byte, int> AsMap(this DynamicBuffer<MyHashMap> buffer)
    {
        return buffer.AsHashMap<MyHashMap, byte, int>();
    }
}
```

Therefore, you can replace your usage with:

```cs
[BurstCompile]
public partial struct MyJob : IJobEntity
{
    public void Execute(DynamicBuffer<MyHashMap> buffer)
    {
        var hashMap = buffer.AsMap();       
    }
}
```
