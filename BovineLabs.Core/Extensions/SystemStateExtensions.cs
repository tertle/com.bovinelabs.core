namespace BovineLabs.Core.Extensions
{
    using BovineLabs.Core.Iterators;
    using Unity.Entities;
    using UnityEngine.ParticleSystemJobs;

    public static class SystemStateExtensions
    {
        public static SharedComponentDataFromIndex<T> GetSharedComponentDataFromIndex<T>(this SystemState system, bool isReadOnly = false)
            where T : struct, ISharedComponentData
        {
            system.AddReaderWriter(isReadOnly ? ComponentType.ReadOnly<T>() : ComponentType.ReadWrite<T>());
            return system.EntityManager.GetSharedComponentDataFromEntity<T>(isReadOnly);
        }
    }
}
