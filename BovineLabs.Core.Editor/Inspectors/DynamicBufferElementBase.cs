// <copyright file="DynamicBufferElementBase.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Inspectors
{
    using System;
    using Unity.Entities;
    using Unity.Entities.UI;

    // public abstract class DynamicBufferElementBase<T> : EntityInspector<DynamicBuffer<T>>
    //     where T : unmanaged, IBufferElementData
    // {
    //     protected DynamicBufferElementBase(object inspector)
    //         : base(inspector)
    //     {
    //         if (inspector is not PropertyInspector<DynamicBuffer<T>>)
    //         {
    //             throw new ArgumentException($"Inspector is not {nameof(PropertyInspector<DynamicBuffer<DynamicBuffer<T>>>)}", nameof(inspector));
    //         }
    //     }
    //
    //     public DynamicBuffer<T> GetBuffer() => this.Context.EntityManager.GetBuffer<T>(this.Context.Entity);
    //
    //     public override bool IsValid()
    //     {
    //         return base.IsValid() && this.Context.EntityManager.HasBuffer<T>(this.Context.Entity);
    //     }
    // }
}