// <copyright file="RuntimeContentCatalogUtility.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Internal
{
    using System.Collections.Generic;
    using Unity.Entities;
    using Unity.Entities.Content;
    using Unity.Entities.Serialization;

    public static class RuntimeContentCatalogUtility
    {
        public static void GetSubScenesAndArchives(string catalogPath, out List<Hash128> scenes, out List<Hash128> archives)
        {
            scenes = new List<Hash128>();
            archives = new List<Hash128>();

            if (!string.IsNullOrEmpty(catalogPath) && BlobAssetReference<RuntimeContentCatalogData>.TryRead(catalogPath, 1, out var catalogData))
            {
                GetSubScenes(catalogData, scenes);
                GetArchives(catalogData, archives);
                catalogData.Dispose();
            }
        }

        public static List<Hash128> GetSubScenes(string catalogPath)
        {
            var scenes = new List<Hash128>();
            if (!string.IsNullOrEmpty(catalogPath) && BlobAssetReference<RuntimeContentCatalogData>.TryRead(catalogPath, 1, out BlobAssetReference<RuntimeContentCatalogData> catalogData))
            {
                GetSubScenes(catalogData, scenes);
                catalogData.Dispose();
            }

            return scenes;
        }

        public static List<Hash128> GetArchives(string catalogPath)
        {
            var archives = new List<Hash128>();
            if (!string.IsNullOrEmpty(catalogPath) && BlobAssetReference<RuntimeContentCatalogData>.TryRead(catalogPath, 1, out BlobAssetReference<RuntimeContentCatalogData> catalogData))
            {
                GetArchives(catalogData, archives);
                catalogData.Dispose();
            }

            return archives;
        }

        private static void GetSubScenes(BlobAssetReference<RuntimeContentCatalogData> catalogData, List<Hash128> scenes)
        {
            for (var i = 0; i < catalogData.Value.Objects.Length; i++)
            {
                var obj = catalogData.Value.Objects[i];
                if (obj.ObjectId.GenerationType != WeakReferenceGenerationType.SubSceneObjectReferences)
                {
                    continue;
                }

                scenes.Add(obj.ObjectId.GlobalId.AssetGUID);
            }
        }

        private static void GetArchives(BlobAssetReference<RuntimeContentCatalogData> catalogData, List<Hash128> archives)
        {
            for (var i = 0; i < catalogData.Value.Archives.Length; i++)
            {
                var archive = catalogData.Value.Archives[i];
                if (!archive.ArchiveId.IsValid)
                {
                    continue;
                }

                archives.Add(archive.ArchiveId.Value);
            }
        }
    }
}
