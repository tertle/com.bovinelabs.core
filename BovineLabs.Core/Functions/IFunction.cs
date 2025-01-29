// <copyright file="IFunction.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Functions
{
    using Unity.Entities;

    public unsafe delegate void UpdateFunction(void* target, ref SystemState state);

    public unsafe delegate void DestroyFunction(void* target, ref SystemState state);

    public unsafe delegate void ExecuteFunction(void* target, void* data, void* result);

    /// <summary> An implementation of a forwarding function pointer for extending jobs to other developers or modders. </summary>
    /// <typeparam name="T"> Is the void* data that will be passed to the ExecuteFunction. Also serves as a grouping mechanism for ReflectAll. </typeparam>
    public interface IFunction<T>
        where T : unmanaged
    {
        /// <summary>
        /// Gets the OnDestroy forwarding function which must be a static forwarding function however it is never burst compiled.
        /// Should be called from a Systems OnDestroy to cleanup any allocated memory.
        /// Optional, return null if not required.
        /// </summary>
        DestroyFunction DestroyFunction { get; }

        /// <summary>
        /// Gets the OnUpdate forwarding function which must be a static forwarding function and burst compilable.
        /// Should be called from a systems OnUpdate and allows you to update Lookups etc if required.
        /// As safety will not work, you must use the provided UnsafeComponentLookup and UnsafeBufferLookup.
        /// Optional, return null if not required.
        /// </summary>
        UpdateFunction UpdateFunction { get; }

        /// <summary>
        /// Gets the OnUpdate forwarding function which must be a static forwarding function and burst compilable.
        /// The logic that will execute inside the job when requested.
        /// </summary>
        ExecuteFunction ExecuteFunction { get; }

        /// <summary> Called directly from the builder to setup the struct if required. </summary>
        /// <param name="state"> The system state. </param>
        void OnCreate(ref SystemState state)
        {
        }
    }
}
