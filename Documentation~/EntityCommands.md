# IEntityCommands
## Summary
Provides a shared interface between EntityManager, EntityCommandBuffer, EntityCommandBuffer.ParallelWriter, and IBaker.

This allows you to write a single method that can use any of the aforementioned Entity manipulators.

- CommandBufferCommands
- CommandBufferParallelCommands
- EntityManagerCommands
- BakerCommands

### Method Definition
```csharp
public static class MyHelperClass
{
    public static void MyCustomMethod<T>(T commands)
        where T : unmanaged, IEntityCommands
    {
        commands.AddComponent(LocalTransform.FromPosition(new float3(1,2,3));
    }
}
```

### Method Invoking
```csharp
var commands = new CommandBufferCommands(commandBuffer, entity);
MyHelperClass.MyCustomMethod(commands);
```
