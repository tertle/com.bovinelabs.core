namespace BovineLabs.Core.Asset
{
    public interface IAutoRefPostProcessor
    {
        void OnAutoRefUpdated(string fieldName);
    }
}
