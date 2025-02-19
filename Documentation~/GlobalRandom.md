# Global Random
## Summary
Global Random is a static random that can be used from bursted  main or worker threads. 

Under the hood it uses ThreadRandom so it creates a per thread Random instance, cache aligned to avoid false sharing, that can be used without locks.

While it is fine for most games, note as it's thread based it is non-deterministic. 

## Example

```csharp
public partial struct TestSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        new TestJob().ScheduleParallel();
    }

    [BurstCompile]
    private partial struct TestJob : IJobEntity
    {
        private void Execute(ref LocalTransform transform)
        {
            transform.Position = GlobalRandom.NextFloat3();
            transform.Scale = GlobalRandom.NextInt();
        }
    }
}
```
