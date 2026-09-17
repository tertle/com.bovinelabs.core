namespace BovineLabs.Core.Internal
{
    using Unity.Entities;
    using Unity.Scripting.LifecycleManagement;

    public static class CompanionComponentSupportedTypes
    {
        [NoAutoStaticsCleanup]
        public static ComponentType[] Types
        {
            get => Unity.Entities.Conversion.CompanionComponentSupportedTypes.Types;
            set => Unity.Entities.Conversion.CompanionComponentSupportedTypes.Types = value;
        }
    }
}
