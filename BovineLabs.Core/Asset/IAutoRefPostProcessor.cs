namespace BovineLabs.Core.Asset
{
    /// <summary> Optional hook for managers that need to restore invariants after AutoRef updates a field. </summary>
    public interface IAutoRefPostProcessor
    {
        /// <summary> Called after AutoRef has updated a manager field. </summary>
        /// <param name="fieldName"> The field that was updated. </param>
        void OnAutoRefUpdated(string fieldName);
    }
}
