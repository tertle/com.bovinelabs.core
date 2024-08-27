// <copyright file="UIGameSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Sample.Extension.UI
{
    using BovineLabs.Core.States;
    using BovineLabs.Core.UI;
    using BovineLabs.Sample.Extension;
    using Unity.Burst;
    using Unity.Entities;

    [UpdateInGroup(typeof(UISystemGroup))]
    public partial struct UIGameSystem : ISystem, ISystemStartStop
    {
        private UIHelper<UIGameBinding, UIGameBinding.Data> helper;

        public void OnCreate(ref SystemState state)
        {
            StateAPI.Register<UIState, State, UIStates>(ref state, "game");
            this.helper = new UIHelper<UIGameBinding, UIGameBinding.Data>("game");
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
            if (this.helper.Binding.Quit.TryConsume())
            {
                GameAPI.StateSet(ref state, "quit");
            }
        }

        private struct State : IComponentData
        {
        }
    }
}
