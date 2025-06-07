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
        public static List<WeakObjectSceneReference> GetSubScenes(string catalogPath)
        {
            var scenes = new List<WeakObjectSceneReference>();

            if (!string.IsNullOrEmpty(catalogPath) && BlobAssetReference<RuntimeContentCatalogData>.TryRead(catalogPath, 1, out var catalogData))
            {

                for (var i = 0; i < catalogData.Value.Objects.Length; i++)
                {
                    var obj = catalogData.Value.Objects[i];
                    if (obj.ObjectId.GenerationType != WeakReferenceGenerationType.SubSceneObjectReferences)
                    {
                        continue;
                    }

                    var untyped = new UntypedWeakReferenceId
                    {
                        GlobalId = obj.ObjectId.GlobalId,
                        GenerationType = obj.ObjectId.GenerationType,
                    };

                    scenes.Add(new WeakObjectSceneReference { Id = untyped });
                }

                catalogData.Dispose();
            }

            return scenes;
        }
    }
}
