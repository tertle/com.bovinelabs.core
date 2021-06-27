// <copyright file="AssetRef.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.ResourceManagement
{
    using Unity.Collections;
    using UnityEngine.AddressableAssets;

    /// <summary> An unmanaged asset reference for addressables. </summary>
    public struct AssetRef : IKeyEvaluator
    {
        private FixedString128 key;

        /// <inheritdoc />
        object IKeyEvaluator.RuntimeKey => this.key.ToString();

        public static implicit operator AssetRef(AssetReference reference)
        {
            return new AssetRef { key = reference.RuntimeKey.ToString() };
        }

        public static implicit operator AssetRef(AssetLabelReference reference)
        {
            return new AssetRef { key = reference.labelString };
        }

        /// <inheritdoc />
        bool IKeyEvaluator.RuntimeKeyIsValid()
        {
            return !this.key.IsEmpty;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return this.key.ToString();
        }
    }
}