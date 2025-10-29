// <copyright file="MemoryLabel.cs" company="BovineLabs">
//         Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !UNITY_6000_3_OR_NEWER
// ReSharper disable once CheckNamespace
namespace Unity.Collections
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.InteropServices;
    using Unity.Collections.LowLevel.Unsafe;

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    public readonly struct MemoryLabel
    {
        [NativeDisableUnsafePtrRestriction]
        private readonly IntPtr pointer;
        internal readonly Allocator allocator;

        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryLabel"/> struct.
        /// </summary>
        /// <param name="areaName">The name of the memory area.</param>
        /// <param name="objectName">The name of the object being labeled.</param>
        /// <param name="allocator">The allocator to use. Defaults to Allocator.Persistent. Only Allocator.Persistent and Allocator.Domain support memory labeling.</param>
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Matching wrap")]
        public MemoryLabel(string areaName, string objectName, Allocator allocator = Allocator.Persistent)
        {
            this.pointer = IntPtr.Zero;
            this.allocator = allocator;
        }

        /// <summary>
        ///                <para>
        /// Determines whether the specified allocator supports memory labeling.
        /// </para>
        ///            </summary>
        /// <param name="allocator">The allocator to check.</param>
        /// <returns>
        ///     <para>True if the allocator supports labeling; otherwise, false.</para>
        /// </returns>
        public static bool SupportsAllocator(Allocator allocator)
        {
            return allocator == Allocator.Persistent || allocator == Allocator.Domain;
        }

        /// <summary>
        ///                <para>
        /// Gets a value indicating whether this memory label has been created.
        /// </para>
        ///            </summary>
        public bool IsCreated => this.allocator != 0;
    }
}
#endif
