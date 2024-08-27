// <copyright file="UIMenuSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Sample.Extension.UI
{
    using BovineLabs.Core;
    using BovineLabs.Core.States;
    using BovineLabs.Core.UI;
    using Unity.Burst;
    using Unity.Entities;

    [UpdateInGroup(typeof(UISystemGroup))]
    [WorldSystemFilter(Worlds.Service)]
    public partial struct UIMenuSystem : ISystem, ISystemStartStop
    {
        private UIHelper<UIMenuBinding, UIMenuBinding.Data> helper;

        public void OnCreate(ref SystemState state)
        {
            StateAPI.Register<UIState, State, UIStates>(ref state, "menu");
            this.helper = new UIHelper<UIMenuBinding, UIMenuBinding.Data>("menu");
        }

        public void OnStartRunning(ref SystemState state)
        {
            this.helper.Load();
        }

        public void OnStopRunning(ref SystemState state)
        {
            this.helper.Unload();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (this.helper.Binding.Play.TryConsume())
            {
                GameAPI.StateSet(ref state, "game");
            }
        }

        private struct State : IComponentData
        {
        }
    }
}
