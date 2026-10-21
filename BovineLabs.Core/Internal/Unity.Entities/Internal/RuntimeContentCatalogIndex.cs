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
            Archives.Count == 0 && Files.Count == 0 && Objects.Count == 0 && Scenes.Count == 0 && Blobs.Count == 0;
    }
}
