// <copyright file="DestroyEntityCommandBufferSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_LIFECYCLE
namespace BovineLabs.Core.LifeCycle
{
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    [UpdateInGroup(typeof(DestroySystemGroup), OrderLast = true)]
    public partial class DestroyEntityCommandBufferSystem : EntityCommandBufferSystem
    {
        /// <inheritdoc cref="EntityCommandBufferSystem.OnCreate" />
        protected override void OnCreate()
        {
            base.OnCreate();

            this.RegisterSingleton<Singleton>(ref this.PendingBuffers, this.World.Unmanaged);
        }

        /// <inheritdoc cref="EndSimulationEntityCommandBufferSystem.Singleton" />
        public unsafe struct Singleton : IComponentData, IECBSingleton
        {
            private UnsafeList<EntityCommandBuffer>* pendingBuffers;
            private AllocatorManager.AllocatorHandle allocator;

            /// <inheritdoc cref="EndSimulationEntityCommandBufferSystem.Singleton.CreateCommandBuffer" />
            public EntityCommandBuffer CreateCommandBuffer(WorldUnmanaged world)
            {
                return EntityCommandBufferSystem.CreateCommandBuffer(ref *this.pendingBuffers, this.allocator, world);
            }

            /// <inheritdoc/>
            void IECBSingleton.SetPendingBufferList(ref UnsafeList<EntityCommandBuffer> buffers)
            {
                this.pendingBuffers = (UnsafeList<EntityCommandBuffer>*)UnsafeUtility.AddressOf(ref buffers);
            }

            /// <inheritdoc/>
            void IECBSingleton.SetAllocator(Allocator allocatorIn)
            {
                this.allocator = allocatorIn;
            }

            /// <inheritdoc/>
            void IECBSingleton.SetAllocator(AllocatorManager.AllocatorHandle allocatorIn)
            {
                this.allocator = allocatorIn;
            }
        }
    }
}
#endif
