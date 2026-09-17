namespace BovineLabs.Core.Asset
{
    /// <summary>
    /// Automatically assigns IDs unique among assets of the same type, including across branch merges.
    /// </summary>
    public interface IUID
    {
        int ID { get; set; }
    }
}
