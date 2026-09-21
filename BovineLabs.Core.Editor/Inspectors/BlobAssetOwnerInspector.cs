namespace BovineLabs.Core.Editor.Inspectors
{
    using System;
    using System.Runtime.InteropServices;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
    using Unity.Entities.UI;
    using UnityEngine.UIElements;

    internal unsafe class BlobAssetOwnerInspector : PropertyInspector<BlobAssetOwner>
    {
        public override VisualElement Build()
        {
            var parent = new VisualElement();

            var ptr = new TextField
            {
                label = DisplayName,
                value = new IntPtr(Target.BlobAssetBatchPtr).ToString(),
            };

            ptr.SetEnabled(false);

            parent.Add(ptr);

            if (Target.IsCreated)
            {
                var wrapper = UnsafeUtility.AsRef<BlobAssetBatchWrapper>(Target.BlobAssetBatchPtr);

                var totalDataSize = new IntegerField
                {
                    label = "TotalDataSize",
                    value = wrapper.TotalDataSize,
                };

                totalDataSize.SetEnabled(false);
                parent.Add(totalDataSize);

                var blobAssetHeaderCount = new IntegerField
                {
                    label = "BlobAssetHeaderCount",
                    value = wrapper.BlobAssetHeaderCount,
                };

                blobAssetHeaderCount.SetEnabled(false);
                parent.Add(blobAssetHeaderCount);

                var refCount = new IntegerField
                {
                    label = "RefCount",
                    value = wrapper.RefCount,
                };

                refCount.SetEnabled(false);
                parent.Add(refCount);
            }

            return parent;
        }

        [StructLayout(LayoutKind.Explicit, Size = 16)]
        private struct BlobAssetBatchWrapper
        {
            [FieldOffset(0)]
            public int TotalDataSize;
            [FieldOffset(4)]
            public int BlobAssetHeaderCount;
            [FieldOffset(8)]
            public int RefCount;
            [FieldOffset(12)]
            public int Padding;
        }
    }
}
