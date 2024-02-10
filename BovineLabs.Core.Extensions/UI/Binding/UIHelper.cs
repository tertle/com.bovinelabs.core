// <copyright file="UIHelper.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_UI
namespace BovineLabs.Core.UI
{
    using System;
    using System.Runtime.InteropServices;
    using BovineLabs.Core.Keys;
    using Unity.Collections.LowLevel.Unsafe;

    public unsafe struct UIHelper<T, TD>
        where T : IBindingObject<TD>, new()
        where TD : unmanaged
    {
        private readonly int stateKey;
        private readonly int priority;

        private GCHandle handle;
        private TD* data;

        public UIHelper(string stateName, int priority = 0)
            : this((byte)K<UIStates>.NameToKey(stateName), priority)
        {
        }

        public UIHelper(byte stateKey, int priority = 0)
        {
            this.stateKey = stateKey;
            this.priority = priority;
            this.handle = default;
            this.data = default;
        }

        public ref TD Binding => ref UnsafeUtility.AsRef<TD>(this.data);

        public void Load()
        {
            var binding = new T();
            UIDocumentManager.Instance.AddPanel(this.stateKey, binding, this.priority);

            this.handle = GCHandle.Alloc(binding, GCHandleType.Pinned);
            this.data = (TD*)UnsafeUtility.AddressOf(ref binding.Value);

            binding.Load();
        }

        public void Unload()
        {
            UIDocumentManager.Instance.RemovePanel(this.stateKey);

            if (this.handle.IsAllocated)
            {
                var obj = (T)this.handle.Target;
                obj.Unload();
                if (obj is IDisposable disposable)
                {
                    disposable.Dispose();
                }

                this.handle.Free();
                this.handle = default;
                this.data = default;
            }
        }

        // Not burst compatible
        public object? GetToolbar()
        {
            return UIDocumentManager.Instance.GetPanel(this.stateKey);
        }
    }
}
#endif
