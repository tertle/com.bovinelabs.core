// <copyright file="VirtualChunkSettings.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Chunks
{
    using System;
    using System.Collections.Generic;
    using BovineLabs.Core.Settings;
    using UnityEngine;

    [ResourceSettings]
    public class VirtualChunkSettings : ScriptableObject, ISettings
    {
        [SerializeField]
        private ChunkMap[] mappings = Array.Empty<ChunkMap>();

        public IReadOnlyList<ChunkMap> Mappings => this.mappings;

        [Serializable]
        public struct ChunkMap
        {
            public string Label;

            // [Range(0, ChunkLinks.MaxGroupIDs - 1)]
            public byte Chunk;
        }
    }
}
