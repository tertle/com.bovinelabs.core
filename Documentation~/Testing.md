# Testing

`BovineLabs.Testing` provides editor-only helpers for isolated ECS tests. Its main entry point is `ECSTestsFixture`, which creates a fresh world and restores global Unity state after each test.

## Assembly setup

`BovineLabs.Testing` has `autoReferenced` disabled. A typical EditMode test asmdef includes:

```json
{
  "name": "MyGame.Tests",
  "references": [
    "BovineLabs.Core",
    "BovineLabs.Testing",
    "MyGame",
    "Unity.Entities",
    "UnityEngine.TestRunner",
    "UnityEditor.TestRunner"
  ],
  "includePlatforms": [
    "Editor"
  ],
  "overrideReferences": true,
  "precompiledReferences": [
    "nunit.framework.dll"
  ],
  "autoReferenced": false,
  "defineConstraints": [
    "UNITY_INCLUDE_TESTS"
  ]
}
```

Add the production and Unity assemblies used by the actual tests.

## `ECSTestsFixture`

The fixture creates one isolated world per test and exposes:

| Member | Purpose |
|---|---|
| `World` | Managed test world |
| `WorldUnmanaged` | Unmanaged view of the same world |
| `Manager` | The world's `EntityManager` |
| `ManagerDebug` | `EntityManagerDebug` for consistency checks |
| `BlobAssetStore` | Shared temporary blob store for baker-style setup and entity-command tests |
| `TestWorldFlags` | Flags of the test world; override for a role world such as `WorldFlags.GameServer` or `WorldFlags.GameClient` |
| `ForceJobsDebugger` | Whether setup forces the Jobs Debugger on; benchmarks override it to return `false` |

Setup also forces the Jobs Debugger on, creates Core debug state, starts from the default player loop, and pushes the test world on the world stack. Teardown destroys any additional worlds, completes tracked jobs, destroys systems, checks entity-manager consistency, pops and disposes the world, disposes the blob store, and restores the previous global state.

```csharp
namespace MyGame.Tests
{
    using BovineLabs.Testing;
    using NUnit.Framework;
    using Unity.Entities;

    public struct Health : IComponentData
    {
        public int Value;
    }

    public sealed class HealthTests : ECSTestsFixture
    {
        [Test]
        public void ComponentCanBeUpdated()
        {
            var entity = this.Manager.CreateEntity();
            this.Manager.AddComponentData(entity, new Health { Value = 10 });

            var health = this.Manager.GetComponentData<Health>(entity);
            health.Value += 5;
            this.Manager.SetComponentData(entity, health);

            Assert.That(this.Manager.GetComponentData<Health>(entity).Value, Is.EqualTo(15));
        }
    }
}
```

If a fixture overrides `Setup()` or `TearDown()`, call the base implementation. Complete or assign every scheduled job before teardown.

Override `IsEditModeTest` only when the test world should use `WorldFlags.Game` instead of `WorldFlags.Editor`, and `TestWorldFlags` for any other role. Benchmarks that set their own Burst and job-worker state override `ForceJobsDebugger` to return `false`, so they measure with the Editor's Jobs Debugger setting.

## Additional worlds

Create every other world a test needs through the fixture instead of constructing `World` directly, so it gets the same teardown checks:

| Member | Purpose |
|---|---|
| `CreateWorld(name, flags)` | Creates an empty world. It is not pushed on the world stack and has no Core debug state, so serialization and transfer tests see only their own entities. |
| `AddWorld(world)` | Adopts a world created elsewhere, such as by `ClientServerBootstrap`. |
| `DisposeWorld(world)` | Destroys an additional world before the test ends, for tests that observe what happens once a world is gone. |

Teardown destroys the remaining additional worlds in reverse order before the test world. Each completes its tracked jobs, destroys its systems, checks entity-manager consistency, and is removed from the player loop before it is disposed. Add a `BLLogger` entity or `BLDebugSystem` to an additional world whose systems log.

```csharp
[Test]
public void SaveRestoresIntoAFreshWorld()
{
    var target = this.CreateWorld("Restore target");

    // Save from this.World, restore into target, then assert on target.EntityManager.
}
```

A test decorated with `[TestLeakDetection]` checks only allocations made within its method, so it creates and disposes its worlds there with `CreateWorld` and `DisposeWorld` instead of using the test world.

## Leak detection

`TestLeakDetectionAttribute` forgives previously recorded leaks, enables native leak detection when needed, and asserts that the decorated test introduces no leaks.

The attribute targets individual methods, not fixtures:

```csharp
namespace MyGame.Tests
{
    using BovineLabs.Testing;
    using NUnit.Framework;
    using Unity.Collections;

    public sealed class NativeAllocationTests
    {
        [Test]
        [TestLeakDetection]
        public void TemporaryArrayIsDisposed()
        {
            var values = new NativeArray<int>(16, Allocator.TempJob);
            try
            {
                values[0] = 42;
                Assert.That(values[0], Is.EqualTo(42));
            }
            finally
            {
                values.Dispose();
            }
        }
    }
}
```

Leak detection supplements explicit ownership checks; it does not replace correct dependency chaining or disposal.

## Math assertions

`AssertMath.AreApproximatelyEqual` compares `float3` or `quaternion` values component by component with a supplied delta:

```csharp
AssertMath.AreApproximatelyEqual(expectedPosition, actualPosition, 0.001f);
AssertMath.AreApproximatelyEqual(expectedRotation, actualRotation, 0.001f);
```

## Reflection helper

`ReflectionTestHelper.SetPrivateField` can set a private instance field during focused compatibility tests. It silently does nothing when the field name is wrong, so verify the resulting behavior rather than treating the helper call itself as an assertion.

## Testing shared entity builders

Use `EntityManagerCommands` with the fixture's `Manager` and `BlobAssetStore` to exercise the same `IEntityCommands` builder used by production bakers or command buffers:

```csharp
using BovineLabs.Core.EntityCommands;

var entity = this.Manager.CreateEntity();
var commands = new EntityManagerCommands(this.Manager, entity, this.BlobAssetStore);
BuildEntity(ref commands);
```

See [Entity commands](EntityCommands.md) for the generic builder pattern.

## Troubleshooting

**The fixture type is unavailable**

Reference `BovineLabs.Testing` and make the test asmdef editor-only with `UNITY_INCLUDE_TESTS`.

**Teardown reports outstanding jobs**

Assign scheduled handles to the system dependency or complete the final test-owned handle before the test returns.

**A later test sees the previous world**

Do not cache `World`, `EntityManager`, lookups, or entities in static fields. Each fixture test receives a new world.

**Leak detection fails on an unrelated allocation**

Narrow the test, dispose all test-owned containers, and ensure asynchronous disposal handles complete before the method exits.

## Related guides

- [Debugging and logging](Debug.md)
- [Entity commands](EntityCommands.md)
- [Collections](Collections.md)
