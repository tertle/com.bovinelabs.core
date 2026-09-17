#if UNITY_NETCODE
namespace BovineLabs.Core.Iterators
{
    using Unity.Netcode;
    using Unity.Netcode.LowLevel.Unsafe;

    public struct DynamicHashCollectionNetcodeFunctionPointers
    {
        public PortableFunctionPointer<GhostComponentSerializer.PostSerializeBufferDelegate> PostSerializeBuffer;
        public PortableFunctionPointer<GhostComponentSerializer.SerializeBufferDelegate> SerializeBuffer;
        public PortableFunctionPointer<GhostComponentSerializer.CopyToFromSnapshotDelegate> CopyFromSnapshot;
        public PortableFunctionPointer<GhostComponentSerializer.CopyToFromSnapshotDelegate> CopyToSnapshot;
        public PortableFunctionPointer<GhostComponentSerializer.RestoreFromBackupDelegate> RestoreFromBackup;
        public PortableFunctionPointer<GhostComponentSerializer.PredictDeltaDelegate> PredictDelta;
        public PortableFunctionPointer<GhostComponentSerializer.DeserializeDelegate> Deserialize;
#if UNITY_EDITOR || NETCODE_DEBUG
        public PortableFunctionPointer<GhostComponentSerializer.ReportPredictionErrorsDelegate> ReportPredictionErrors;
#endif
    }
}
#endif
