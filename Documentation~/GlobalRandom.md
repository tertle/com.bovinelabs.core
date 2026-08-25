# GlobalRandom

`GlobalRandom` provides one `Unity.Mathematics.Random` state per Unity execution thread. It is Burst-compatible and avoids sharing a mutable random state between parallel workers.

```csharp
using BovineLabs.Core.Utility;
```

## When to use it

Use `GlobalRandom` for independent, non-deterministic effects such as visual variation, debug sampling, or gameplay where replaying the exact sequence is not required.

Do not use it when results must be deterministic across runs, machines, worker counts, or prediction rollbacks. In particular, avoid it for:

- NetCode prediction and rollback
- deterministic simulation or replays
- save data that expects a reproducible sequence
- tests that assert exact random values
- per-entity sequences that must remain stable when scheduling changes

For those cases, store an explicit seed or `Random` state with the owning entity/system and derive independent streams from stable identifiers and ticks.

## Current API

The static wrappers mirror these `Unity.Mathematics.Random` families:

- `NextBool`, `NextBool2`, `NextBool3`, `NextBool4`
- `NextInt`, `NextInt2`, `NextInt3`, `NextInt4`
- `NextUInt`, `NextUInt2`, `NextUInt3`, `NextUInt4`
- `NextFloat`, `NextFloat2`, `NextFloat3`, `NextFloat4`
- `NextDouble`, `NextDouble2`, `NextDouble3`, `NextDouble4`
- `NextFloat2Direction`, `NextFloat3Direction`
- `NextDouble2Direction`, `NextDouble3Direction`
- `NextQuaternionRotation`

Integer, unsigned-integer, float, and double families include their current maximum and minimum/maximum overloads. `NextQuaternion` and `NextFloat3InUnitSphere` are not `GlobalRandom` methods.

For an operation not wrapped by the static API, access the current thread's generator by reference and consume it immediately:

```csharp
ref var random = ref GlobalRandom.Thread;
var value = random.NextFloat();
```

Do not cache that reference across job executions, threads, or frames; `Thread` resolves the state for the thread that accesses it.

## Parallel job example

```csharp
public struct RandomMovement : IComponentData
{
    public float3 MinBounds;
    public float3 MaxBounds;
    public float MinScale;
    public float MaxScale;
}

[BurstCompile]
public partial struct RandomMovementSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        state.Dependency = new RandomMovementJob().ScheduleParallel(state.Dependency);
    }

    [BurstCompile]
    private partial struct RandomMovementJob : IJobEntity
    {
        private void Execute(ref LocalTransform transform, in RandomMovement movement)
        {
            transform.Position = GlobalRandom.NextFloat3(movement.MinBounds, movement.MaxBounds);
            transform.Rotation = GlobalRandom.NextQuaternionRotation();
            transform.Scale = GlobalRandom.NextFloat(movement.MinScale, movement.MaxScale);
        }
    }
}
```

## Initialization and lifetime

Core initializes the thread-local states automatically during editor initialization and player subsystem registration. Callers do not create, seed, or dispose the global pool.

The sequence is intentionally process/domain scoped. Thread scheduling determines which state an invocation consumes, so changing job batching, worker count, or execution order changes the result even with identical input data.

## Related guides

- [Jobs](Jobs.md) describes Core's custom job types.
- [Utility](Utility.md) summarizes other runtime helpers and their ownership rules.
