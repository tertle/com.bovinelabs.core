// <copyright file="ObjectDefinitionProcessor.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.AssetManagement
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using BovineLabs.Core.AssetManagement;
    using JetBrains.Annotations;
    using UnityEditor;

    /// <summary> Ensures <see cref="ObjectDefinition" /> always have a unique ID even if 2 branches merge. </summary>
    public class ObjectDefinitionProcessor : AssetPostprocessor
    {
        [UsedImplicitly(ImplicitUseKindFlags.Access)]
        [SuppressMessage("ReSharper", "Unity.IncorrectMethodSignature", Justification = "Changed in 2021")]
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths,
            bool didDomainReload)
        {
            if (didDomainReload)
            {
                return;
            }

            var didChange = false;

            Dictionary<int, int> map = null;

            foreach (var importedAsset in importedAssets)
            {
                var asset = AssetDatabase.LoadAssetAtPath<ObjectDefinition>(importedAsset);

                if (asset == null)
                {
                    continue;
                }

                map ??= GetIDMap();
                map.TryGetValue(asset.ID, out var count);

                if (count > 1)
                {
                    var newId = GetFirstFreeID(map);
                    map[asset.ID] = count - 1; // update the old ID
                    asset.ID = new ObjectId { ID = newId };
                    map[newId] = 1;
                    EditorUtility.SetDirty(asset);
                    AssetDatabase.SaveAssetIfDirty(asset);

                    didChange = true;
                }
            }

            if (didChange)
            {
                // TODO figure out how to re-import the correct subscene
            }
        }

        private static Dictionary<int, int> GetIDMap()
        {
            var idMap = new Dictionary<int, int>();

            var guids = AssetDatabase.FindAssets($"t:{nameof(ObjectDefinition)}");

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<ObjectDefinition>(path);

                idMap.TryGetValue(asset.ID, out var count);
                count++;
                idMap[asset.ID] = count;
            }

            return idMap;
        }

        private static int GetFirstFreeID(IReadOnlyDictionary<int, int> map)
        {
            for (var i = 0; i < int.MaxValue; i++)
            {
                if (!map.ContainsKey(i))
                {
                    return i;
                }
            }

            return -1; // You'd have to hit int.MaxValue ids to ever hit this case
        }
    }
}
