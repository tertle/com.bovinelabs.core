// <copyright file="HybridComponent.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_HYBRID
namespace BovineLabs.Core.Hybrid
{
    using System;
    using Unity.Entities;
    using UnityEngine;
    using Object = UnityEngine.Object;

    /// <summary> A game object with life cycle managed for you automatically. </summary>
    public class HybridComponent : IComponentData, ICloneable, IDisposable
    {
        public GameObject? Value;

        public object Clone()
        {
            return new HybridComponent { Value = Object.Instantiate(this.Value) };
        }

        public void Dispose()
        {
            Object.Destroy(this.Value);
        }
    }
}
#endif
