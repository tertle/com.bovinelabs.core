namespace BovineLabs.Core.Tests.Utility
{
    using BovineLabs.Core.Extensions;
    using BovineLabs.Core.Utility;
    using BovineLabs.Testing;
    using NUnit.Framework;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Jobs;

    public partial class QueryGroupTests : ECSTestsFixture
    {
        [Test]
        public void Registry_DeduplicatesMembersAndPreservesEnableableBufferFlags()
        {
            using var members = TypeManagerUtil.GetQueryGroupComponents<RegistryTarget>(Allocator.Temp);
            CollectionAssert.AreEquivalent(new[] { ComponentType.ReadOnly<RegistryMember>(), ComponentType.ReadOnly<RegistryBuffer>() }, members);
            TypeManagerUtil.GetQueryGroupComponents<RegistryTarget>(Allocator.Temp, out var enableable, out var normal);
            CollectionAssert.AreEqual(new[] { ComponentType.ReadOnly<RegistryBuffer>() }, enableable);
            CollectionAssert.AreEqual(new[] { ComponentType.ReadOnly<RegistryMember>() }, normal);
        }

        [Test]
        public void Registry_NoGroup_ReturnsEmptyArray()
        {
            using var members = TypeManagerUtil.GetQueryGroupComponents<NoGroup>(Allocator.Temp);
            Assert.AreEqual(0, members.Length);
        }

        [Test]
        public void Registry_FromBurstJob_ReturnsSameMembers()
        {
            using var result = new NativeArray<ComponentType>(2, Allocator.TempJob);
            new RegistryJob {
                Result = result
            }.Schedule().Complete();
            CollectionAssert.AreEquivalent(new[] { ComponentType.ReadOnly<RegistryMember>(), ComponentType.ReadOnly<RegistryBuffer>() }, result);
        }

        [Test]
        public void NoneGroup_ReadOnlyTarget_ExcludesEnabledMembers()
        {
            var plain = Manager.CreateEntity(typeof(Target));
            Manager.CreateEntity(typeof(Target), typeof(NormalMember));
            var disabled = Manager.CreateEntity(typeof(Target), typeof(EnabledMember));
            Manager.SetComponentEnabled<EnabledMember>(disabled, false);
            Manager.CreateEntity(typeof(Target), typeof(EnabledMember));
            using var query = new EntityQueryBuilder(Allocator.Temp).WithAll<Target>().WithNoneQueryGroup<Target>().Build(Manager);
            using var entities = query.ToEntityArray(Allocator.Temp);
            CollectionAssert.AreEquivalent(new[] { plain, disabled }, entities);
        }

        [Test]
        public void AnyGroup_MatchesMembersWithoutTargetAndRespectsEnabledState()
        {
            var normal = Manager.CreateEntity(typeof(NormalMember));
            var buffer = Manager.CreateEntity(typeof(RegistryBuffer));
            var disabled = Manager.CreateEntity(typeof(EnabledMember));
            Manager.SetComponentEnabled<EnabledMember>(disabled, false);
            Manager.CreateEntity(typeof(Target));
            using var query = new EntityQueryBuilder(Allocator.Temp).WithAnyQueryGroup<Target>().Build(Manager);
            using var entities = query.ToEntityArray(Allocator.Temp);
            CollectionAssert.AreEquivalent(new[] { normal, buffer }, entities);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Filter_OnlyWritableTargetsExcludeUnmentionedMembers(bool writable)
        {
            Manager.CreateEntity(typeof(Target));
            Manager.CreateEntity(typeof(Target), typeof(NormalMember));
            using var builder = new EntityQueryBuilder(Allocator.Temp);
            if (writable)
            {
                builder.WithAllRW<Target>();
            }
            else
            {
                builder.WithAll<Target>();
            }

            using var query = builder.WithQueryGroupFilter().Build(Manager);
            Assert.AreEqual(writable ? 1 : 2, query.CalculateEntityCount());
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Filter_ExplicitMemberIsAllowedInEitherAccessMode(bool writableMember)
        {
            Manager.CreateEntity(typeof(Target), typeof(NormalMember));
            using var builder = new EntityQueryBuilder(Allocator.Temp).WithAllRW<Target>();
            if (writableMember)
            {
                builder.WithAllRW<NormalMember>();
            }
            else
            {
                builder.WithAll<NormalMember>();
            }

            using var query = builder.WithQueryGroupFilter().Build(Manager);
            Assert.AreEqual(1, query.CalculateEntityCount());
        }

        [Test]
        public void Filter_ReadOnlyGroupsAllowTransitiveMembersAndTerminateCycles()
        {
            Manager.CreateEntity(typeof(Target), typeof(ChainRoot), typeof(ChainLeaf));
            using var query = new EntityQueryBuilder(Allocator.Temp).WithAllRW<Target>().WithAll<ChainRoot>()
                .WithQueryGroupFilter().Build(Manager);
            Assert.AreEqual(1, query.CalculateEntityCount());
        }

        [Test]
        public void Filter_ExplicitAbsentEnableableMemberRetainsAbsentSemantics()
        {
            Manager.CreateEntity(typeof(Target));
            var disabled = Manager.CreateEntity(typeof(Target), typeof(EnabledMember));
            Manager.SetComponentEnabled<EnabledMember>(disabled, false);
            using var query = new EntityQueryBuilder(Allocator.Temp).WithAllRW<Target>().WithAbsent<EnabledMember>()
                .WithQueryGroupFilter().Build(Manager);
            Assert.AreEqual(1, query.CalculateEntityCount());
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Filter_PresentAndDisabledWritableTargetsExcludeOverrides(bool disabledTarget)
        {
            var plain = Manager.CreateEntity(typeof(EnabledTarget));
            var overridden = Manager.CreateEntity(typeof(EnabledTarget), typeof(EnabledTargetMember));
            Manager.SetComponentEnabled<EnabledTarget>(plain, false);
            Manager.SetComponentEnabled<EnabledTarget>(overridden, false);
            using var builder = new EntityQueryBuilder(Allocator.Temp);
            if (disabledTarget)
            {
                builder.WithDisabledRW<EnabledTarget>();
            }
            else
            {
                builder.WithPresentRW<EnabledTarget>();
            }

            using var query = builder.WithQueryGroupFilter().Build(Manager);
            Assert.AreEqual(1, query.CalculateEntityCount());
        }

        [Test]
        public void Filter_AdditionalDescriptionsAreFilteredIndependently()
        {
            Manager.CreateEntity(typeof(Target));
            Manager.CreateEntity(typeof(Target), typeof(NormalMember));
            Manager.CreateEntity(typeof(NoGroup));
            using var query = new EntityQueryBuilder(Allocator.Temp).WithAllRW<Target>().WithQueryGroupFilter()
                .AddAdditionalQuery().WithAll<NoGroup>().WithQueryGroupFilter().Build(Manager);
            Assert.AreEqual(2, query.CalculateEntityCount());
        }

        [Test]
        public void Matcher_OptionalEnableableMembersRespectComponentAndBufferState()
        {
            var handle = World.CreateSystem<MatcherSystem>();
            ref var state = ref WorldUnmanaged.ResolveSystemStateRef(handle);
            using var matcher = new QueryGroupMatcher<Target>(ref state);
            var missing = Manager.CreateEntity(typeof(Target));
            Assert.IsTrue(matcher.Matches(Manager.GetChunk(missing)).AllFalse);
            var enabled = Manager.CreateEntity(typeof(Target), typeof(EnabledMember));
            Assert.IsTrue(matcher.Matches(Manager.GetChunk(enabled))[0]);
            Manager.SetComponentEnabled<EnabledMember>(enabled, false);
            Assert.IsTrue(matcher.Matches(Manager.GetChunk(enabled)).AllFalse);
            var buffer = Manager.CreateEntity(typeof(Target), typeof(RegistryBuffer));
            Assert.IsTrue(matcher.Matches(Manager.GetChunk(buffer))[0]);
            Manager.SetComponentEnabled<RegistryBuffer>(buffer, false);
            Assert.IsTrue(matcher.Matches(Manager.GetChunk(buffer)).AllFalse);
        }

        [Test]
        public void Matcher_NormalMemberMatchesPrefabChunks()
        {
            var handle = World.CreateSystem<MatcherSystem>();
            ref var state = ref WorldUnmanaged.ResolveSystemStateRef(handle);
            using var matcher = new QueryGroupMatcher<Target>(ref state);
            var entity = Manager.CreateEntity(typeof(Target), typeof(NormalMember), typeof(Prefab));
            Assert.IsTrue(matcher.Matches(Manager.GetChunk(entity)).AllTrue);
        }

        [Test]
        public void Matcher_NoGroup_MatchesNothing()
        {
            var handle = World.CreateSystem<MatcherSystem>();
            ref var state = ref WorldUnmanaged.ResolveSystemStateRef(handle);
            using var matcher = new QueryGroupMatcher<NoGroup>(ref state);
            var entity = Manager.CreateEntity(typeof(NoGroup));
            Assert.IsTrue(matcher.Matches(Manager.GetChunk(entity)).AllFalse);
        }

        [BurstCompile(CompileSynchronously = true)]
        private struct RegistryJob : IJob
        {
            public NativeArray<ComponentType> Result;

            public void Execute()
            {
                var members = TypeManagerUtil.GetQueryGroupComponents<RegistryTarget>(Allocator.Temp);
                Result.CopyFrom(members);
            }
        }

        private partial struct MatcherSystem : ISystem
        {
            public void OnUpdate(ref SystemState state)
            {
            }
        }

        private struct Target : IComponentData { }
        private struct RegistryTarget : IComponentData { }
        private struct NoGroup : IComponentData { }
        private struct EnabledTarget : IComponentData, IEnableableComponent { }

        [QueryGroup(typeof(RegistryTarget)), QueryGroup(typeof(RegistryTarget))]
        private struct RegistryMember : IComponentData { }

        [QueryGroup(typeof(RegistryTarget)), QueryGroup(typeof(Target))]
        private struct RegistryBuffer : IBufferElementData, IEnableableComponent
        {
            public byte Value;
        }

        [QueryGroup(typeof(Target))]
        private struct NormalMember : IComponentData { }

        [QueryGroup(typeof(Target))]
        private struct EnabledMember : IComponentData, IEnableableComponent { }

        [QueryGroup(typeof(EnabledTarget))]
        private struct EnabledTargetMember : IComponentData { }

        [QueryGroup(typeof(ChainLeaf))]
        private struct ChainRoot : IComponentData { }

        [QueryGroup(typeof(ChainRoot)), QueryGroup(typeof(Target))]
        private struct ChainMember : IComponentData { }

        [QueryGroup(typeof(ChainMember)), QueryGroup(typeof(Target))]
        private struct ChainLeaf : IComponentData { }
    }
}
