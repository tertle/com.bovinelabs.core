namespace BovineLabs.Core.Iterators
{
    using System.Diagnostics.CodeAnalysis;
    using Unity.Entities;

    [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Defines memory layout")]
    public interface IDynamicUntypedBuffer : IBufferElementData
    {
        byte Value { get; }
    }
}
