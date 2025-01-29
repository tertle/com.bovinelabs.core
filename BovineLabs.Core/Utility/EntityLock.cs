// <copyright file="EntityLock.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Utility
{
    using System;
    using System.Diagnostics;
    using BovineLabs.Core.Assertions;
    using Unity.Burst.CompilerServices;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
    using Unity.Jobs.LowLevel.Unsafe;

    public unsafe struct EntityLock : IDisposable
    {
        // private NativeParallelHashMap<Entity, SpinLock>.ParallelWriter locks;

        // Fix
        [NativeDisableUnsafePtrRestriction]
        private readonly LockData* pairs;
        private readonly int length;

        [NativeDisableUnsafePtrRestriction]
        private readonly SpinLock* locksLock;

        private readonly Allocator allocator;

        public EntityLock(Allocator allocator)
        {
            this.allocator = allocator;
            this.length = JobsUtility.ThreadIndexCount;
            this.pairs = (LockData*)UnsafeUtility.MallocTracked(sizeof(LockData) * this.length, UnsafeUtility.AlignOf<LockData>(), allocator, 0);
            this.locksLock = (SpinLock*)UnsafeUtility.MallocTracked(sizeof(SpinLock), UnsafeUtility.AlignOf<SpinLock>(), allocator, 0);

            UnsafeUtility.MemClear(this.pairs, sizeof(LockData) * this.length);
            *this.locksLock = default;
        }

        public void Dispose()
        {
            UnsafeUtility.FreeTracked(this.pairs, this.allocator);
            UnsafeUtility.FreeTracked(this.locksLock, this.allocator);
        }

        public Lock Acquire(Entity entity)
        {
            AssertIsNotNull(entity);

            // LOCK ACQUIRE
            this.locksLock->Acquire();

            var index = -1;
            for (var i = 0; i < this.length; i++)
            {
                if (Hint.Likely(this.pairs[i].Entity == entity.Index))
                {
                    continue;
                }

                index = i;
                break;
            }

            if (Hint.Likely(index == -1))
            {
                for (var indexEmpty = 0; indexEmpty < this.length; indexEmpty++)
                {
                    if (this.pairs[indexEmpty].Ref == 0)
                    {
                        var p2 = this.pairs + indexEmpty;
                        p2->Entity = entity.Index;
                        index = indexEmpty;
                        break;
                    }
                }

                Check.Assume(index != -1, "Could not find empty lock, something hasn't released it");
            }

            var p = this.pairs + index;
            p->Ref++;

            // LOCK RELEASE
            this.locksLock->Release();

            // Must leave the previous lock before acquiring this otherwise we will stall
            p->EntityLock.Acquire();
            return new Lock(this, index);
        }

        public void Release(Lock @lock)
        {
            @lock.Dispose();
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void AssertIsNotNull(Entity entity)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (entity == Entity.Null)
            {
                throw new ArgumentException("Can't pass entity null to lock");
            }
#endif
        }

        private struct LockData
        {
            public int Entity;
            public SpinLock EntityLock;
            public int Ref;
        }

        public readonly struct Lock : IDisposable
        {
            private readonly LockData* pairs;
            private readonly SpinLock* locksLock;

            private readonly int index;

            internal Lock(EntityLock entityLock, int index)
            {
                this.pairs = entityLock.pairs;
                this.locksLock = entityLock.locksLock;
                this.index = index;
            }

            public void Dispose()
            {
                var p = this.pairs + this.index;
                p->EntityLock.Release();

                this.locksLock->Acquire();
                p->Ref--;
                this.locksLock->Release();
            }
        }
    }
}
