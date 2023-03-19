// <copyright file="EndDestroyCommandBufferSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Destroy
{
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    [UpdateInGroup(typeof(DestroySystemGroup), OrderLast = true)]
    public unsafe partial class EndDestroyCommandBufferSystem : EntityCommandBufferSystem
    {
        protected override void OnCreate()
        {
            base.OnCreate();

            this.RegisterSingleton<Singleton>(ref *this.m_PendingBuffers, this.World.Unmanaged);
        }

        public struct Singleton : IComponentData, IECBSingleton
        {
            private UnsafeList<EntityCommandBuffer>* pendingBuffers;
            private Allocator allocator;

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
        }
    }
}
