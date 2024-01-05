// <copyright file="UIHelper.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_UI
namespace BovineLabs.Core.UI
{
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
        {
            this.stateKey = (byte)K<UIStates>.NameToKey(stateName);
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
        }

        public void Unload()
        {
            UIDocumentManager.Instance.RemovePanel(this.stateKey);

            if (this.handle.IsAllocated)
            {
                this.handle.Free();
                this.handle = default;
                this.data = default;
            }
        }
    }
}
#endif
