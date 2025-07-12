# GlobalRandom

## Summary

GlobalRandom is a static random number generator that can be used from bursted main or worker threads. It provides a thread-safe, high-performance solution for generating random numbers in Unity DOTS systems without the need for manual synchronization.

**Key Features:**
- Thread-safe random number generation
- Burst-compatible for optimal performance
- Cache-aligned per-thread instances to avoid false sharing
- No locks or synchronization primitives required
- Implements all Unity.Mathematics.Random operations

**Important:** GlobalRandom is non-deterministic due to its thread-based nature. For deterministic behavior (e.g., in networked games), use per-entity random seeds instead.

## Architecture

Under the hood, GlobalRandom uses ThreadRandom, which creates a per-thread Random instance. Each thread gets its own cache-aligned random number generator to avoid false sharing and contention between threads.

## Available Methods

GlobalRandom implements all Unity.Mathematics.Random operations as static methods:

```cs
// Basic random numbers
float NextFloat();
int NextInt();
uint NextUInt();
bool NextBool();

// Vector types
float2 NextFloat2();
float3 NextFloat3();
float4 NextFloat4();

// Directional vectors
float2 NextFloat2Direction();
float3 NextFloat3Direction();
float3 NextFloat3InUnitSphere();

// Quaternions
quaternion NextQuaternion();
```

All methods support the same overloads as Unity.Mathematics.Random (min/max ranges, etc.).

## Usage Example

```cs
[BurstCompile]
public partial struct RandomMovementSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        new RandomMovementJob().ScheduleParallel();
    }

    [BurstCompile]
    private partial struct RandomMovementJob : IJobEntity
    {
        private void Execute(ref LocalTransform transform, in RandomMovementComponent movement)
        {
            // Generate random position within bounds
            transform.Position = GlobalRandom.NextFloat3(movement.MinBounds, movement.MaxBounds);
            
            // Generate random rotation
            transform.Rotation = GlobalRandom.NextQuaternion();
            
            // Generate random scale
            transform.Scale = GlobalRandom.NextFloat(movement.MinScale, movement.MaxScale);
        }
    }
}
```

## Determinism Considerations

GlobalRandom is inherently non-deterministic due to thread-based execution. Thread scheduling affects the order of execution, making results unpredictable across runs.

## Threading and Safety

GlobalRandom uses thread-local storage for thread safety, making it safe to use in parallel jobs without synchronization. Each thread gets its own random instance, preventing contention and ensuring optimal performance in multi-threaded scenarios.