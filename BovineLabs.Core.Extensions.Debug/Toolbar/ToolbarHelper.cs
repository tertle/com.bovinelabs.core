// <copyright file="ToolbarHelper.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_TOOLBAR
namespace BovineLabs.Core.Toolbar
{
    using System;
    using System.Runtime.InteropServices;
    using BovineLabs.Core.Keys;
    using BovineLabs.Core.UI;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
    using UnityEngine;

    public unsafe struct ToolbarHelper<T, TD>
        where T : class, IBindingObject<TD>, new()
        where TD : unmanaged
    {
        private readonly FixedString32Bytes tabName;
        private readonly FixedString32Bytes groupName;
        private readonly int assetKey;

        private int key;

        private GCHandle handle;
        private TD* data;

        public ToolbarHelper(ref SystemState state, FixedString32Bytes tabName, FixedString32Bytes groupName, int assetKey)
        {
            this.tabName = tabName;
            this.groupName = groupName;
            this.assetKey = assetKey;
            this.handle = default;
            this.data = default;
            this.key = 0;

            if (this.assetKey == -1)
            {
                state.Enabled = false;
            }
        }

        public ToolbarHelper(ref SystemState state, FixedString32Bytes tabName, FixedString32Bytes groupName, FixedString32Bytes assetKey)
            : this(ref state, tabName, groupName, K<ToolbarStates>.TryNameToKey(assetKey, out var n) ? n : -1)
        {
        }

        public ToolbarHelper(ref SystemState state, FixedString32Bytes groupName, int assetKey)
            : this(ref state, FormatWorld(state.World.Name), groupName, assetKey)
        {
        }

        public ToolbarHelper(ref SystemState state, FixedString32Bytes groupName, FixedString32Bytes assetKey)
            : this(ref state, FormatWorld(state.World.Name), groupName, K<ToolbarStates>.TryNameToKey(assetKey, out var n) ? n : -1)
        {
        }

        public bool AssetKeyValid => this.assetKey != -1;

        public ref TD Binding => ref UnsafeUtility.AsRef<TD>(this.data);

        // Load the tab onto the group. Usually called from OnStartRunning.
        public void Load()
        {
            ToolbarManager.Instance.AddGroup<T>(this.tabName.ToString(), this.groupName.ToString(), this.assetKey, out this.key, out var binding);

            this.handle = GCHandle.Alloc(binding, GCHandleType.Pinned);
            this.data = (TD*)UnsafeUtility.AddressOf(ref binding.Value);

            binding.Load();
        }

        public void Unload()
        {
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
            else
            {
                Debug.LogError("Did not unload");
            }

            ToolbarManager.Instance.RemoveGroup(this.key);
        }

        public bool IsVisible()
        {
            return ToolbarManagerData.ActiveTab.Data == this.tabName;
        }

        // Not burst compatible
        public object? GetToolbar()
        {
            return ToolbarManager.Instance.GetPanel(this.assetKey);
        }

        private static string FormatWorld(string name)
        {
            return name.EndsWith("World") ? name[..name.LastIndexOf("World", StringComparison.Ordinal)] : name;
        }
    }
}
#endif
