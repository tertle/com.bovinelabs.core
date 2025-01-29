// <copyright file="ObjectPool.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Collections
{
    using System;
    using System.Collections.Generic;

    /// <summary> Generic object pool. </summary>
    /// <typeparam name="T"> Type of the object pool. </typeparam>
    public class ObjectPool<T> : IDisposable
        where T : new()
    {
        private readonly Func<T> create;
        private readonly Action<T> onDispose;
        private readonly Action<T> onGet;
        private readonly Action<T> onRelease;
        private readonly Stack<T> stack = new();

        /// <summary> Initializes a new instance of the <see cref="ObjectPool{T}" /> class. </summary>
        /// <param name="create"> Optional func for creating objects. </param>
        /// <param name="onDispose"> Optional callback when disposing the object pool. </param>
        /// <param name="actionOnGet"> Optional callback when an element is retrieved. </param>
        /// <param name="actionOnRelease"> Optional callback when an element is returned. </param>
        public ObjectPool(Func<T> create = null, Action<T> onDispose = null, Action<T> actionOnGet = null, Action<T> actionOnRelease = null)
        {
            this.create = create;
            this.onDispose = onDispose;
            this.onGet = actionOnGet;
            this.onRelease = actionOnRelease;
        }

        /// <summary> Gets number of objects in the pool. </summary>
        public int CountAll { get; private set; }

        /// <summary> Gets number of active objects in the pool. </summary>
        public int CountActive => this.CountAll - this.CountInactive;

        /// <summary> Gets number of inactive objects in the pool. </summary>
        public int CountInactive => this.stack.Count;

        public void Dispose()
        {
            if (this.onDispose == null)
            {
                return;
            }

            foreach (var element in this.stack)
            {
                this.onDispose.Invoke(element);
            }
        }

        /// <summary> Get an object from the pool. </summary>
        /// <returns> A new object from the pool. </returns>
        public T Get()
        {
            T element;
            if (this.stack.Count == 0)
            {
                element = this.create == null ? new T() : this.create.Invoke();
                this.CountAll++;
            }
            else
            {
                element = this.stack.Pop();
            }

            this.onGet?.Invoke(element);
            return element;
        }

        /// <summary> Get et new PooledObject. </summary>
        /// <param name="v"> Output new typed object. </param>
        /// <returns> New PooledObject. </returns>
        public PooledObject Get(out T v)
        {
            return new PooledObject(v = this.Get(), this);
        }

        /// <summary> Release an object to the pool. </summary>
        /// <param name="element"> Object to release. </param>
        public void Release(T element)
        {
            // keep heavy checks in editor
#if BL_POOL_CHECKS
            if (this.stack.Count > 0)
            {
                if (this.stack.Contains(element))
                {
                    Debug.LogError("Internal error. Trying to destroy object that is already released to pool.");
                }
            }
#endif
            this.onRelease?.Invoke(element);
            this.stack.Push(element);
        }

        /// <summary>
        /// Pooled object.
        /// </summary>
        public struct PooledObject : IDisposable
        {
            public readonly T Item;
            public readonly ObjectPool<T> Pool;

            internal PooledObject(T value, ObjectPool<T> pool)
            {
                this.Item = value;
                this.Pool = pool;
            }

            /// <summary> Disposable pattern implementation. </summary>
            void IDisposable.Dispose()
            {
                this.Pool.Release(this.Item);
            }
        }
    }
}
