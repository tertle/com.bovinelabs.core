// <copyright file="SubSceneToolbarSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_TOOLBAR && !BL_DISABLE_SUBSCENE
namespace BovineLabs.Core.ToolbarTabs
{
    using BovineLabs.Core.Collections;
    using BovineLabs.Core.LifeCycle;
    using BovineLabs.Core.SubScenes;
    using BovineLabs.Core.Toolbar;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Scenes;

    [UpdateInGroup(typeof(ToolbarSystemGroup))]
    public partial struct SubSceneToolbarSystem : ISystem, ISystemStartStop
    {
        private ToolbarHelper<SubSceneToolbarBindings, SubSceneToolbarBindings.Data> toolbar;

        private NativeList<Entity> subScenes;
        private NativeList<FixedString64Bytes> subScenesBuffer;
        private BitArray256 subSceneMask;

        /// <inheritdoc/>
        public void OnCreate(ref SystemState state)
        {
            this.toolbar = new ToolbarHelper<SubSceneToolbarBindings, SubSceneToolbarBindings.Data>(ref state, "SubScene", "subscene");
            this.subScenes = new NativeList<Entity>(16, Allocator.Persistent);
            this.subScenesBuffer = new NativeList<FixedString64Bytes>(Allocator.Persistent);
        }

        /// <inheritdoc/>
        public void OnDestroy(ref SystemState state)
        {
            this.subScenes.Dispose();
            this.subScenesBuffer.Dispose();
        }

        /// <inheritdoc/>
        public void OnStartRunning(ref SystemState state)
        {
            this.toolbar.Load();
            ref var data = ref this.toolbar.Binding;

            data.SubScenes = new NativeList<FixedString64Bytes>(Allocator.Persistent);
        }

        /// <inheritdoc/>
        public void OnStopRunning(ref SystemState state)
        {
            ref var data = ref this.toolbar.Binding;
            data.SubScenes.Dispose();
            this.toolbar.Unload();
        }

        /// <inheritdoc/>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!this.toolbar.IsVisible())
            {
                return;
            }

            ref var data = ref this.toolbar.Binding;
            var newMask = default(BitArray256);
            this.subScenesBuffer.Clear();
            this.subScenes.Clear();

            foreach (var (_, e) in SystemAPI.Query<RefRO<SceneReference>>().WithNone<RequiredSubScene>().WithEntityAccess())
            {
                var index = this.subScenes.Length;
                this.subScenes.Add(e);

                state.EntityManager.GetName(e, out var name);
                if (name == default)
                {
                    name = e.ToFixedString();
                }

                this.subScenesBuffer.Add(name);

                var loaded = SceneSystem.IsSceneLoaded(state.WorldUnmanaged, e);
                newMask[(uint)index] = loaded;
            }

            if (this.subSceneMask != newMask)
            {
                data.SubSceneMask = this.subSceneMask = newMask;
            }
            else if (this.subSceneMask != data.SubSceneMask)
            {
                this.subSceneMask = data.SubSceneMask;

                var ecb = SystemAPI.GetSingleton<InstantiateCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
                var resolvedSectionEntitys = SystemAPI.GetBufferLookup<ResolvedSectionEntity>(true);

                for (var index = 0; index < this.subScenes.Length; index++)
                {
                    var entity = this.subScenes[index];

                    var isOpen = this.subSceneMask[(uint)index];

                    if (isOpen == SceneSystem.IsSceneLoaded(state.WorldUnmanaged, entity))
                    {
                        continue;
                    }

                    if (isOpen)
                    {
                        SubSceneUtil.LoadScene(ecb, entity, ref resolvedSectionEntitys);
                    }
                    else
                    {
                        SubSceneUtil.UnloadScene(ecb, entity, ref resolvedSectionEntitys);
                    }
                }
            }

            if (!this.subScenesBuffer.AsArray().ArraysEqual(data.SubScenes.AsArray()))
            {
                data.SubScenes.Clear();
                data.SubScenes.AddRange(this.subScenesBuffer.AsArray());
                data.SubScenes = data.SubScenes; // notify
            }
        }
    }
}
#endif
