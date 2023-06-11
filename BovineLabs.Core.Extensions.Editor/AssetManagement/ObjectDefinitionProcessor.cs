// <copyright file="ObjectDefinitionProcessor.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_OBJECT_DEFINITION
namespace BovineLabs.Core.Editor.AssetManagement
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using BovineLabs.Core.ObjectManagement;
    using JetBrains.Annotations;
    using UnityEditor;
    using UnityEngine;

    /// <summary> An <see cref="AssetPostprocessor"/> that ensures <see cref="IID" /> types always have a unique ID even if 2 branches merge. </summary>
    public class ObjectDefinitionProcessor : AssetPostprocessor
    {
        private interface IProcessor
        {
            bool DidChange { get; }

            bool TryProcess(string importedAsset);
        }

        [UsedImplicitly(ImplicitUseKindFlags.Access)]
        [SuppressMessage("ReSharper", "Unity.IncorrectMethodSignature", Justification = "Changed in 2021")]
        private static void OnPostprocessAllAssets(
            string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
        {
            if (didDomainReload)
            {
                return;
            }

            var processors = new IProcessor[]
            {
                new Processor<ObjectDefinition>(),
                new Processor<ObjectGroup>(),
            };

            foreach (var asset in importedAssets)
            {
                foreach (var p in processors)
                {
                    if (p.TryProcess(asset))
                    {
                        break;
                    }
                }
            }

            foreach (var p in processors)
            {
                if (p.DidChange)
                {
                    // TODO dirty subscenes?
                }
            }
        }

        private class Processor<T> : IProcessor
            where T : ScriptableObject, IID
        {
            private Dictionary<int, int>? map;

            public bool DidChange { get; private set; }

            public bool TryProcess(string importedAsset)
            {
                var asset = AssetDatabase.LoadAssetAtPath<T>(importedAsset);

                if (asset == null)
                {
                    return false;
                }

                this.map ??= GetIDMap();
                this.map.TryGetValue(asset.ID, out var count);

                if (count > 1)
                {
                    var newId = GetFirstFreeID(this.map);
                    this.map[asset.ID] = count - 1; // update the old ID
                    asset.ID = new ObjectId { ID = newId };
                    this.map[newId] = 1;
                    EditorUtility.SetDirty(asset);
                    AssetDatabase.SaveAssetIfDirty(asset);

                    this.DidChange = true;
                }

                return true;
            }

            private static Dictionary<int, int> GetIDMap()
            {
                var idMap = new Dictionary<int, int>();

                var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");

                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var asset = AssetDatabase.LoadAssetAtPath<T>(path);

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
}
#endif
