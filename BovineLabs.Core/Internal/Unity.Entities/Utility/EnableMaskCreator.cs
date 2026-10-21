namespace BovineLabs.Core.Utility
{
    using Unity.Entities;

    public static unsafe class EnableMaskCreator
    {
        public static EnabledMask Create(SafeBitRef enableBitRef, int* ptrChunkDisabledCount)
        {
            return new EnabledMask(enableBitRef, ptrChunkDisabledCount);
        }
    }
}
