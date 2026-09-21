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
        [NativeDisableUnsafePtrRestriction]
        private readonly LockData* _pairs;
        private readonly int _length;

        [NativeDisableUnsafePtrRestriction]
        private readonly SpinLock* _locksLock;

        private readonly Allocator _allocator;

        public EntityLock(Allocator allocator)
        {
            _allocator = allocator;
            _length = JobsUtility.ThreadIndexCount;
            _pairs = (LockData*)UnsafeUtility.MallocTracked(sizeof(LockData) * _length, UnsafeUtility.AlignOf<LockData>(), allocator, 0);
            _locksLock = (SpinLock*)UnsafeUtility.MallocTracked(sizeof(SpinLock), UnsafeUtility.AlignOf<SpinLock>(), allocator, 0);

            UnsafeUtility.MemClear(_pairs, sizeof(LockData) * _length);
            *_locksLock = default;
        }

        public void Dispose()
        {
            UnsafeUtility.FreeTracked(_pairs, _allocator);
            UnsafeUtility.FreeTracked(_locksLock, _allocator);
        }

        public Lock Acquire(Entity entity)
        {
            AssertIsNotNull(entity);

            // LOCK ACQUIRE
            _locksLock->Acquire();

            var index = -1;
            for (var i = 0; i < _length; i++)
            {
                if (Hint.Likely(_pairs[i].Entity == entity.Index))
                {
                    continue;
                }

                index = i;
                break;
            }

            if (Hint.Likely(index == -1))
            {
                for (var indexEmpty = 0; indexEmpty < _length; indexEmpty++)
                {
                    if (_pairs[indexEmpty].Ref == 0)
                    {
                        var p2 = _pairs + indexEmpty;
                        p2->Entity = entity.Index;
                        index = indexEmpty;
                        break;
                    }
                }

                Check.Assume(index != -1, "Could not find empty lock, something hasn't released it");
            }

            var p = _pairs + index;
            p->Ref++;

            // LOCK RELEASE
            _locksLock->Release();

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
            private readonly LockData* _pairs;
            private readonly SpinLock* _locksLock;

            private readonly int _index;

            internal Lock(EntityLock entityLock, int index)
            {
                _pairs = entityLock._pairs;
                _locksLock = entityLock._locksLock;
                _index = index;
            }

            public void Dispose()
            {
                var p = _pairs + _index;
                p->EntityLock.Release();

                _locksLock->Acquire();
                p->Ref--;
                _locksLock->Release();
            }
        }
    }
}
