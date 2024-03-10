// <copyright file="UnityTimeSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_TIME
namespace BovineLabs.Core.Time
{
    using Unity.Burst;
    using Unity.Core;
    using Unity.Entities;
    using Unity.Mathematics;
    using UnityEngine;

    [UpdateInGroup(typeof(InitializationSystemGroup), OrderFirst = true)]
    [UpdateAfter(typeof(BeginInitializationEntityCommandBufferSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.ThinClientSimulation | Worlds.Service)]
    [DisableAutoCreation]
    public partial struct UpdateWorldTimeSystem : ISystem, ISystemStartStop
    {
        private Entity timeSingleton;

        /// <inheritdoc/>
        public unsafe void OnCreate(ref SystemState state)
        {
            var timeTypes = stackalloc ComponentType[2];
            timeTypes[0] = ComponentType.ReadWrite<WorldTime>();
            timeTypes[1] = ComponentType.ReadWrite<WorldTimeQueue>();
            this.timeSingleton = state.EntityManager.CreateEntity(state.EntityManager.CreateArchetype(timeTypes, 2));
        }

        [BurstCompile]
        public void OnStartRunning(ref SystemState state)
        {
            var currentElapsedTime = SystemAPI.Time.ElapsedTime;
            var deltaTime = math.min(Time.deltaTime, state.WorldUnmanaged.MaximumDeltaTime);
            var timeData = new TimeData(elapsedTime: currentElapsedTime - deltaTime, deltaTime: deltaTime);

            this.SetTime(ref state, timeData);
        }

        public void OnStopRunning(ref SystemState state)
        {
        }

        /// <inheritdoc/>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var currentElapsedTime = SystemAPI.Time.ElapsedTime;
            var deltaTime = math.min(Time.deltaTime, state.WorldUnmanaged.MaximumDeltaTime);
            this.SetTime(ref state, new TimeData(elapsedTime: currentElapsedTime + deltaTime, deltaTime: deltaTime));
        }

        private void SetTime(ref SystemState state, TimeData newTimeData)
        {
            state.EntityManager.SetComponentData(timeSingleton, new WorldTime {Time = newTimeData});
            state.WorldUnmanaged.Time = newTimeData;
        }
    }
}
#endif
