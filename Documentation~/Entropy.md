# Entropy
## Summary
Entropy provides an easy way to manage generating Random values in jobs by providing a per thread Random instance. 

While it is fine for most games, note as it's thread based it is non-deterministic. 

## Example

```csharp
public partial struct TestSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        new TestJob { Random = SystemAPI.GetSingleton<Entropy>().Random }.ScheduleParallel();
    }

    [BurstCompile]
    private partial struct TestJob : IJobEntity
    {
        public ThreadRandom Random;

        private void Execute(ref LocalTransform transform)
        {
            // Make sure to store by ref
            ref Random random = ref this.Random.GetRandomRef();
            transform.Position = random.NextFloat3();

            // Working directly is fine as well
            transform.Scale = this.Random.GetRandomRef().NextInt();
        }
    }
}
```
