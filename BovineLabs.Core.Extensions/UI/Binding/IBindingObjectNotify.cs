// <copyright file="IBindingObjectNotify.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_UI
namespace BovineLabs.Core.UI
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using UnityEngine.UIElements;

    public delegate void OnPropertyChangedDelegate(IntPtr target, in FixedString64Bytes property);

    public interface IBindingObjectNotify : IBindingObject, INotifyBindablePropertyChanged
    {
        void OnPropertyChanged(in FixedString64Bytes property);

        internal static class Active
        {
            public static readonly Dictionary<IntPtr, IBindingObjectNotify> Objects = new();

            public static FunctionPointer<OnPropertyChangedDelegate> Notify;

            static Active()
            {
                Notify = new FunctionPointer<OnPropertyChangedDelegate>(Marshal.GetFunctionPointerForDelegate<OnPropertyChangedDelegate>(NotifyForwarding));
            }

            [AOT.MonoPInvokeCallback(typeof(OnPropertyChangedDelegate))]
            private static void NotifyForwarding(IntPtr target, in FixedString64Bytes property)
            {
                if (Objects.TryGetValue(target, out var notify))
                {
                    notify.OnPropertyChanged(property);
                }
            }
        }
    }

    public interface IBindingObjectNotify<T> : IBindingObject<T>, IBindingObjectNotify
        where T : unmanaged, IBindingObjectNotifyData
    {
        public static unsafe void Load(IBindingObjectNotify<T> bindingObjectNotify)
        {
            var addr = (IntPtr)UnsafeUtility.AddressOf(ref bindingObjectNotify.Value);
            Active.Objects[addr] = bindingObjectNotify;
            bindingObjectNotify.Value.Notify = Active.Notify;
        }

        public static unsafe void Unload(IBindingObjectNotify<T> bindingObjectNotify)
        {
            var addr = (IntPtr)UnsafeUtility.AddressOf(ref bindingObjectNotify.Value);
            Active.Objects.Remove(addr);
            bindingObjectNotify.Value.Notify = default;
        }

        /// <inheritdoc/>
        void IBindingObject.Load()
        {
            Load(this);
        }

        /// <inheritdoc/>
        void IBindingObject.Unload()
        {
            Unload(this);
        }
    }

    public interface IBindingObjectNotifyData
    {
        FunctionPointer<OnPropertyChangedDelegate> Notify { get; set; }
    }

    public static class BindingObjectNotifyDataExtensions
    {
        public static unsafe void Notify<T>(this ref T binding, [CallerMemberName] string property = "")
            where T : unmanaged, IBindingObjectNotifyData
        {
            if (binding.Notify.IsCreated)
            {
                binding.Notify.Invoke((IntPtr)UnsafeUtility.AddressOf(ref binding), property);
            }
        }

        public static unsafe void NotifyExplicit<T>(this ref T binding, FixedString64Bytes property)
            where T : unmanaged, IBindingObjectNotifyData
        {
            if (binding.Notify.IsCreated)
            {
                binding.Notify.Invoke((IntPtr)UnsafeUtility.AddressOf(ref binding), property);
            }
        }
    }
}
#endif
