// <copyright file="IBindingObjectNotify.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_UI
namespace BovineLabs.Core.UI
{
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using Unity.Burst;
    using Unity.Collections;
    using UnityEngine.UIElements;

    public delegate void OnPropertyChangedDelegate(in FixedString64Bytes property);

    public static class BindingObjectCollection
    {
        public static readonly Dictionary<IBindingObject, GCHandle> Handles = new();
    }

    public interface IBindingObjectNotify<T> : IBindingObject<T>, INotifyBindablePropertyChanged
        where T : unmanaged, IBindingObjectNotifyData
    {
        /// <inheritdoc/>
        void IBindingObject<T>.Load()
        {
            var propertyChanged = new OnPropertyChangedDelegate(this.OnPropertyChanged);
            BindingObjectCollection.Handles[this] = GCHandle.Alloc(propertyChanged, GCHandleType.Pinned);
            this.Value.Notify = new FunctionPointer<OnPropertyChangedDelegate>(Marshal.GetFunctionPointerForDelegate(propertyChanged));
        }

        /// <inheritdoc/>
        void IBindingObject<T>.Unload()
        {
            if (BindingObjectCollection.Handles.Remove(this, out var handle))
            {
                handle.Free();
            }

            this.Value.Notify = default;
        }

        void OnPropertyChanged(in FixedString64Bytes property);
    }

    public interface IBindingObjectNotifyData
    {
        FunctionPointer<OnPropertyChangedDelegate> Notify { get; set; }
    }

    public static class BindingObjectNotifyDataExtensions
    {
        public static void Notify<T>(this ref T binding, [CallerMemberName] string property = "")
            where T : unmanaged, IBindingObjectNotifyData
        {
            if (binding.Notify.IsCreated)
            {
                binding.Notify.Invoke(property);
            }
        }
    }
}
#endif
