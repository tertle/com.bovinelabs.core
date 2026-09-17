namespace BovineLabs.Core.Collections
{
    public unsafe interface INativeStreamReader
    {
        int ForEachCount { get; }

        int RemainingItemCount { get; }

        int BeginForEachIndex(int foreachIndex);

        void EndForEachIndex();

        byte* ReadUnsafePtr(int size);

        ref T Read<T>()
            where T : unmanaged;

        int Count();

        void ReadLarge(byte* buffer, int size);

        void ReadLarge<T>(byte* buffer, int length)
            where T : unmanaged;
    }
}
