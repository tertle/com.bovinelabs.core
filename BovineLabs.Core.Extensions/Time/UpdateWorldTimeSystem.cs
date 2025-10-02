// <copyright file="UpdateWorldTimeSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_TIME
namespace BovineLabs.Core
{
    using BovineLabs.Core.Extensions;
    using Unity.Burst;
    using Unity.Core;
    using Unity.Entities;
    using Unity.Mathematics;
    using UnityEngine;

    /// <summary> Replaces the <see cref="UpdateWorldTimeSystem" /> but burst compiles it and adds support to Service world. </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderFirst = true)]
    [UpdateAfter(typeof(BeginInitializationEntityCommandBufferSystem))]
    [WorldSystemFilter(Worlds.All)]
    public partial struct UpdateWorldTimeSystem : ISystem, ISystemStartStop
    {
        private Entity timeSingleton;

        /// <inheritdoc />
        public unsafe void OnCreate(ref SystemState state)
        {
            var timeTypes = stackalloc ComponentType[2];
            timeTypes[0] = ComponentType.ReadWrite<WorldTime>();
            timeTypes[1] = ComponentType.ReadWrite<WorldTimeQueue>();
            this.timeSingleton = state.EntityManager.CreateEntity(state.EntityManager.CreateArchetype(timeTypes, 2));

            if (state.WorldUnmanaged.SystemExists<Unity.Entities.UpdateWorldTimeSystem>())
            {
                state.WorldUnmanaged.GetExistingSystemState<Unity.Entities.UpdateWorldTimeSystem>().Enabled = false;
            }
        }

        /// <inheritdoc />
        [BurstCompile]
        public void OnStartRunning(ref SystemState state)
        {
            var currentElapsedTime = SystemAPI.Time.ElapsedTime;
            var deltaTime = math.min(Time.deltaTime, state.WorldUnmanaged.MaximumDeltaTime);
            var timeData = new TimeData(currentElapsedTime - deltaTime, deltaTime);

            this.SetTime(ref state, timeData);
        }

        /// <inheritdoc />
        public void OnStopRunning(ref SystemState state)
        {
        }

        /// <inheritdoc />
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
#if UNITY_EDITOR
            if (state.WorldUnmanaged.SystemExists<Unity.Entities.UpdateWorldTimeSystem>())
            {
                state.WorldUnmanaged.GetExistingSystemState<Unity.Entities.UpdateWorldTimeSystem>().Enabled = false;
            }
#endif
            var currentElapsedTime = SystemAPI.Time.ElapsedTime;
            var deltaTime = math.min(Time.deltaTime, state.WorldUnmanaged.MaximumDeltaTime);
            this.SetTime(ref state, new TimeData(currentElapsedTime + deltaTime, deltaTime));
        }

        private void SetTime(ref SystemState state, TimeData newTimeData)
        {
            state.EntityManager.SetComponentData(this.timeSingleton, new WorldTime { Time = newTimeData });
            state.WorldUnmanaged.Time = newTimeData;
        }
    }
}
#endif
