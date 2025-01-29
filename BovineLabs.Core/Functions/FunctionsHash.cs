// <copyright file="FunctionsHash.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Functions
{
    using System;
    using System.Runtime.InteropServices;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    /// <summary> The collection of forwarding functions that can be executed in a burst job. </summary>
    /// <typeparam name="T"> Is the void* data that will be passed to the ExecuteFunction. Also serves as a grouping mechanism for ReflectAll. </typeparam>
    /// <typeparam name="TO"> Is the type of result that is expected from the ExecuteFunction. </typeparam>
    public unsafe struct FunctionsHash<T, TO>
        where T : unmanaged
        where TO : unmanaged
    {
        [ReadOnly]
        private NativeHashMap<long, FunctionData> functions;

        /// <summary> Initializes a new instance of the <see cref="FunctionsHash{T, TO}" /> struct. </summary>
        /// <param name="functions"> The collection of functions. </param>
        internal FunctionsHash(NativeHashMap<long, FunctionData> functions)
        {
            this.functions = functions;
        }

        /// <summary> Gets the number of functions for iterating. </summary>
        public int Length => this.functions.Count;

        /// <summary> Call this in OnDestroy on the system to dispose memory. It also calls OnDestroy on all IFunction. </summary>
        /// <param name="state"> The system state. </param>
        public void OnDestroy(ref SystemState state)
        {
            using var e = this.functions.GetEnumerator();
            while (e.MoveNext())
            {
                var d = e.Current.Value;
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
            using var e = this.functions.GetEnumerator();
            while (e.MoveNext())
            {
                var d = e.Current.Value;
                if (d.UpdateFunction.IsCreated)
                {
                    d.UpdateFunction.Invoke(d.Target, ref state);
                }
            }
        }

        /// <summary> Call to execute a specific function. </summary>
        /// <param name="hash"> The hash of function to call. </param>
        /// <param name="data"> The data to pass to the function. </param>
        /// <param name="result"> The result from the function. </param>
        /// <returns> If the function was found. </returns>
        public bool TryExecute(long hash, ref T data, out TO result)
        {
            if (!this.functions.TryGetValue(hash, out var e))
            {
                result = default;
                return false;
            }

            var ptr = UnsafeUtility.AddressOf(ref data);

            TO resultValue = default;
            e.ExecuteFunction.Invoke(e.Target, ptr, &resultValue);
            result = resultValue;
            return true;
        }
    }
}
