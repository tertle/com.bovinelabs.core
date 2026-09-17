namespace BovineLabs.Core.Asset
{
    /// <summary>
    /// Marking a scriptable object with this interface will automatically generate a branch safe unique ID for all objects regardless of type.
    /// </summary>
    public interface IUIDGlobal : IUID
    {
    }
}
