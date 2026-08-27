// <copyright file="StableAssetId.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Authoring
{
    using System;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Core;
    using Unity.Entities;
    using UnityEditor;
    using Object = UnityEngine.Object;

    /// <summary> Creates stable project-scoped identifiers for persisted authoring assets. </summary>
    public static class StableAssetId
    {
        /// <summary> Creates a stable identifier from an asset GUID and local file ID. </summary>
        /// <param name="asset"> The persisted asset or sub-asset. </param>
        /// <returns> A stable nonzero identifier. </returns>
        public static unsafe ulong Create(Object asset)
        {
            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string assetGuid, out long localFileId) &&
                !string.IsNullOrEmpty(assetGuid) && localFileId != 0)
            {
                var identity = new AssetIdentity
                {
                    AssetGuid = new Hash128(assetGuid),
                    LocalFileId = localFileId,
                };

                var id = XXHash.Hash64((byte*)UnsafeUtility.AddressOf(ref identity), UnsafeUtility.SizeOf<AssetIdentity>());
                if (id == 0)
                {
                    throw new InvalidOperationException($"Authoring asset '{asset.name}' resolved to the reserved stable ID 0.");
                }

                return id;
            }

#if UNITY_INCLUDE_TESTS
            // Tests construct transient ScriptableObjects; import workers must never bake their session-local IDs.
            if (!AssetDatabase.IsAssetImportWorkerProcess())
            {
                return UnityEngine.EntityId.ToULong(asset.GetEntityId());
            }
#endif

            throw new InvalidOperationException($"Authoring asset '{asset.name}' must be persisted before its stable ID is read.");
        }

        private struct AssetIdentity
        {
            internal Hash128 AssetGuid;
            internal long LocalFileId;
        }
    }
}
