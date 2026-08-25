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
            if (!string.IsNullOrEmpty(catalogPath) &&
                BlobAssetReference<RuntimeContentCatalogData>.TryRead(
                    catalogPath, 1, out BlobAssetReference<RuntimeContentCatalogData> catalogData))
            {
                GetSubScenes(catalogData, scenes);
                catalogData.Dispose();
            }

            return scenes;
        }

        public static List<Hash128> GetArchives(string catalogPath)
        {
            var archives = new List<Hash128>();
            if (!string.IsNullOrEmpty(catalogPath) &&
                BlobAssetReference<RuntimeContentCatalogData>.TryRead(
                    catalogPath, 1, out BlobAssetReference<RuntimeContentCatalogData> catalogData))
            {
                GetArchives(catalogData, archives);
                catalogData.Dispose();
            }

            return archives;
        }

        internal static bool TryReadIndex(string catalogPath, out RuntimeContentCatalogIndex index, out string diagnostic)
        {
            index = new RuntimeContentCatalogIndex();
            diagnostic = string.Empty;

            if (string.IsNullOrWhiteSpace(catalogPath) ||
                !BlobAssetReference<RuntimeContentCatalogData>.TryRead(catalogPath, 1, out var catalogData))
            {
                diagnostic = $"Runtime content catalog is missing or malformed: {catalogPath}";
                return false;
            }

            try
            {
                ref var data = ref catalogData.Value;
                if (!TryIndexArchives(ref data, index, out diagnostic) || !TryIndexFiles(ref data, index, out diagnostic) ||
                    !TryIndexObjects(ref data, index, out diagnostic) || !TryIndexScenes(ref data, index, out diagnostic) ||
                    !TryIndexBlobs(ref data, index, out diagnostic) || !TryValidateDependencies(ref data, out diagnostic))
                {
                    return false;
                }

                return true;
            }
            finally
            {
                catalogData.Dispose();
            }
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

        private static bool TryIndexArchives(ref RuntimeContentCatalogData data, RuntimeContentCatalogIndex index, out string diagnostic)
        {
            var hasBuiltInArchive = false;
            for (var i = 0; i < data.Archives.Length; i++)
            {
                var id = data.Archives[i].ArchiveId.Value;
                if (!id.IsValid)
                {
                    if (hasBuiltInArchive)
                    {
                        diagnostic = "Catalog contains duplicate built-in archive entries.";
                        return false;
                    }

                    hasBuiltInArchive = true;
                    continue;
                }

                if (!index.Archives.Add(id))
                {
                    diagnostic = $"Catalog contains duplicate archive ID {id}.";
                    return false;
                }
            }

            diagnostic = string.Empty;
            return true;
        }

        private static bool TryIndexFiles(ref RuntimeContentCatalogData data, RuntimeContentCatalogIndex index, out string diagnostic)
        {
            var hasBuiltInFile = false;
            for (var i = 0; i < data.Files.Length; i++)
            {
                var file = data.Files[i];
                var id = file.FileId.Value;
                if (file.ArchiveIndex < 0 || file.ArchiveIndex >= data.Archives.Length)
                {
                    diagnostic = $"Catalog file {id} has invalid archive index {file.ArchiveIndex}.";
                    return false;
                }

                if (file.DependencyIndex < 0 || file.DependencyIndex >= data.Dependencies.Length)
                {
                    diagnostic = $"Catalog file {id} has invalid dependency index {file.DependencyIndex}.";
                    return false;
                }

                if (!id.IsValid)
                {
                    if (data.Archives[file.ArchiveIndex].ArchiveId.IsValid)
                    {
                        diagnostic = $"Catalog built-in file {i} does not reference the built-in archive.";
                        return false;
                    }

                    if (hasBuiltInFile)
                    {
                        diagnostic = "Catalog contains duplicate built-in file entries.";
                        return false;
                    }

                    hasBuiltInFile = true;
                    continue;
                }

                if (!data.Archives[file.ArchiveIndex].ArchiveId.IsValid)
                {
                    diagnostic = $"Catalog file {id} references the built-in archive.";
                    return false;
                }

                if (!index.Files.Add(id))
                {
                    diagnostic = $"Catalog contains duplicate file ID {id}.";
                    return false;
                }
            }

            diagnostic = string.Empty;
            return true;
        }

        private static bool TryIndexObjects(ref RuntimeContentCatalogData data, RuntimeContentCatalogIndex index, out string diagnostic)
        {
            for (var i = 0; i < data.Objects.Length; i++)
            {
                var item = data.Objects[i];
                var id = new UntypedWeakReferenceId(item.ObjectId.GlobalId, item.ObjectId.GenerationType);
                if (!TryAddLocation(id, item.FileIndex, data.Files.Length, index.Objects, "object", out diagnostic))
                {
                    return false;
                }
            }

            diagnostic = string.Empty;
            return true;
        }

        private static bool TryIndexScenes(ref RuntimeContentCatalogData data, RuntimeContentCatalogIndex index, out string diagnostic)
        {
            for (var i = 0; i < data.Scenes.Length; i++)
            {
                var item = data.Scenes[i];
                var id = new UntypedWeakReferenceId(item.SceneId.GlobalId, item.SceneId.GenerationType);
                if (!TryAddLocation(id, item.FileIndex, data.Files.Length, index.Scenes, "scene", out diagnostic))
                {
                    return false;
                }
            }

            diagnostic = string.Empty;
            return true;
        }

        private static bool TryIndexBlobs(ref RuntimeContentCatalogData data, RuntimeContentCatalogIndex index, out string diagnostic)
        {
            for (var i = 0; i < data.Blobs.Length; i++)
            {
                var item = data.Blobs[i];
                var id = new UntypedWeakReferenceId(item.ObjectId.GlobalId, item.ObjectId.GenerationType);
                if (!TryAddLocation(id, item.FileIndex, data.Files.Length, index.Blobs, "blob", out diagnostic))
                {
                    return false;
                }

                if (item.Offset < 0 || item.Length < 0)
                {
                    diagnostic = $"Catalog blob {id} has an invalid offset or length.";
                    return false;
                }
            }

            diagnostic = string.Empty;
            return true;
        }

        private static bool TryValidateDependencies(ref RuntimeContentCatalogData data, out string diagnostic)
        {
            for (var setIndex = 0; setIndex < data.Dependencies.Length; setIndex++)
            {
                ref var dependencies = ref data.Dependencies[setIndex];
                for (var dependencyIndex = 0; dependencyIndex < dependencies.Length; dependencyIndex++)
                {
                    var fileIndex = dependencies[dependencyIndex];
                    if (fileIndex < 0 || fileIndex >= data.Files.Length)
                    {
                        diagnostic = $"Catalog dependency set {setIndex} contains invalid file index {fileIndex}.";
                        return false;
                    }
                }
            }

            diagnostic = string.Empty;
            return true;
        }

        private static bool TryAddLocation(
            UntypedWeakReferenceId id, int fileIndex, int fileCount, HashSet<UntypedWeakReferenceId> ids, string category, out string diagnostic)
        {
            if (!id.IsValid)
            {
                diagnostic = $"Catalog {category} has an invalid ID.";
                return false;
            }

            if (fileIndex < 0 || fileIndex >= fileCount)
            {
                diagnostic = $"Catalog {category} {id} has invalid file index {fileIndex}.";
                return false;
            }

            if (!ids.Add(id))
            {
                diagnostic = $"Catalog contains duplicate {category} ID {id}.";
                return false;
            }

            diagnostic = string.Empty;
            return true;
        }
    }
}
