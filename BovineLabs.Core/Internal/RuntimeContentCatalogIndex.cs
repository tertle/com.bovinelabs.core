// <copyright file="RuntimeContentCatalogIndex.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Internal
{
    using System.Collections.Generic;
    using Unity.Entities;
    using Unity.Entities.Serialization;

    internal sealed class RuntimeContentCatalogIndex
    {
        public HashSet<Hash128> Archives { get; } = new();

        public HashSet<Hash128> Files { get; } = new();

        public HashSet<UntypedWeakReferenceId> Objects { get; } = new();

        public HashSet<UntypedWeakReferenceId> Scenes { get; } = new();

        public HashSet<UntypedWeakReferenceId> Blobs { get; } = new();

        public bool IsEmpty =>
            this.Archives.Count == 0 && this.Files.Count == 0 && this.Objects.Count == 0 && this.Scenes.Count == 0 && this.Blobs.Count == 0;
    }
}
