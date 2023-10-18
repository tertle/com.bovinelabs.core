// <copyright file="FunctionData.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Functions
{
    using System;
    using Unity.Burst;

    internal unsafe struct FunctionData
    {
        public void* Target;

        public IntPtr DestroyFunction;
        public FunctionPointer<ExecuteFunction> ExecuteFunction;
        public FunctionPointer<UpdateFunction> UpdateFunction;
    }
}
