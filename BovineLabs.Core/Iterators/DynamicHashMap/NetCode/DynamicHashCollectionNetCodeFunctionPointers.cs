// <copyright file="DynamicHashCollectionNetCodeFunctionPointers.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_NETCODE
namespace BovineLabs.Core.Iterators
{
    using Unity.NetCode;
    using Unity.NetCode.LowLevel.Unsafe;

    /// <summary>Concrete Burst function pointers generated for a dynamic hash collection serializer.</summary>
    public struct DynamicHashCollectionNetCodeFunctionPointers
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
