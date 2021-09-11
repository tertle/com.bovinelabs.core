// <copyright file="AsyncOperationExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Extensions
{
    using System;
    using System.Threading.Tasks;
    using UnityEngine;

    public static class AsyncOperationExtensions
    {
        public static async Task Task(this AsyncOperation operation, IProgress<float> progress = null)
        {
            while (!operation.isDone)
            {
                await System.Threading.Tasks.Task.Delay(1);

                progress?.Report(operation.progress);
            }
        }
    }
}
