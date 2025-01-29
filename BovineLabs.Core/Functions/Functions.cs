// <copyright file="Functions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Functions
{
    using System;
    using System.Runtime.InteropServices;
    using BovineLabs.Core.Extensions;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    /// <summary> The collection of forwarding functions that can be executed in a burst job. </summary>
    /// <typeparam name="T"> Is the void* data that will be passed to the ExecuteFunction. Also serves as a grouping mechanism for ReflectAll. </typeparam>
    /// <typeparam name="TO"> Is the type of result that is expected from the ExecuteFunction. </typeparam>
    public unsafe struct Functions<T, TO>
        where T : unmanaged
        where TO : unmanaged
    {
        [ReadOnly]
        private NativeArray<FunctionData> functions;

        /// <summary> Initializes a new instance of the <see cref="Functions{T, TO}" /> struct. </summary>
        /// <param name="functions"> The collection of functions. </param>
        internal Functions(NativeArray<FunctionData> functions)
        {
            this.functions = functions;
        }

        /// <summary> Gets the number of functions for iterating. </summary>
        public int Length => this.functions.Length;

        /// <summary> Call this in OnDestroy on the system to dispose memory. It also calls OnDestroy on all IFunction. </summary>
        /// <param name="state"> The system state. </param>
        public void OnDestroy(ref SystemState state)
        {
            foreach (var d in this.functions)
            {
                if (d.DestroyFunction != IntPtr.Zero)
                {
                    Marshal.GetDelegateForFunctionPointer<DestroyFunction>(d.DestroyFunction).Invoke(d.Target, ref state);
                }

                UnsafeUtility.FreeTracked(d.Target, Allocator.Persistent);
            }

            this.functions.Dispose();
        }

        /// <summary> Call in OnUpdate to call OnUpdate on all IFunction. </summary>
        /// <param name="state"> The system state. </param>
        public void Update(ref SystemState state)
        {
            foreach (var d in this.functions)
            {
                if (d.UpdateFunction.IsCreated)
                {
                    d.UpdateFunction.Invoke(d.Target, ref state);
                }
            }
        }

        /// <summary> Call to execute a specific function. </summary>
        /// <param name="index"> The index of function to call. Should be positive and less than Length. </param>
        /// <param name="data"> The data to pass to the function. </param>
        /// <returns> The result. </returns>
        public TO Execute(int index, ref T data)
        {
            ref var e = ref this.functions.ElementAt(index);
            var ptr = UnsafeUtility.AddressOf(ref data);

            TO result = default;
            e.ExecuteFunction.Invoke(e.Target, ptr, &result);
            return result;
        }
    }
}
