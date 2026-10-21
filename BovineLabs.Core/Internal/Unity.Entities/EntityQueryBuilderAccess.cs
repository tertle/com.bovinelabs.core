namespace BovineLabs.Core.Internal
{
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    // Keep access to the engine-owned builder data inside the Entities friend assembly.
    public static unsafe class EntityQueryBuilderAccess
    {
        public static ref UnsafeList<ComponentType> GetAll(EntityQueryBuilder builder)
        {
            return ref builder._builderDataPtr->_all;
        }

        public static ref UnsafeList<ComponentType> GetAny(EntityQueryBuilder builder)
        {
            return ref builder._builderDataPtr->_any;
        }

        public static ref UnsafeList<ComponentType> GetNone(EntityQueryBuilder builder)
        {
            return ref builder._builderDataPtr->_none;
        }

        public static ref UnsafeList<ComponentType> GetDisabled(EntityQueryBuilder builder)
        {
            return ref builder._builderDataPtr->_disabled;
        }

        public static ref UnsafeList<ComponentType> GetAbsent(EntityQueryBuilder builder)
        {
            return ref builder._builderDataPtr->_absent;
        }

        public static ref UnsafeList<ComponentType> GetPresent(EntityQueryBuilder builder)
        {
            return ref builder._builderDataPtr->_present;
        }

        public static void Invalidate(EntityQueryBuilder builder)
        {
            builder._builderDataPtr->_isFinalized = 0;
        }
    }
}
