namespace BovineLabs.Core.Utility
{
    using Unity.Collections;
    using Unity.Entities;

    public static unsafe class TypeManagerUtil
    {
        public static NativeArray<ComponentType> GetQueryGroupComponents<T>(Allocator allocator)
        {
            return GetQueryGroupComponents(ComponentType.ReadOnly<T>(), allocator);
        }

        public static NativeArray<ComponentType> GetQueryGroupComponents(ComponentType componentType, Allocator allocator)
        {
            var count = TypeManagerEx.GetQueryGroupCount(componentType.TypeIndex);
            var groups = TypeManagerEx.GetQueryGroups(componentType.TypeIndex);
            var components = new NativeArray<ComponentType>(count, allocator);
            for (var i = 0; i < count; i++)
            {
                components[i] = ComponentType.ReadOnly(groups[i]);
            }

            return components;
        }

        public static void GetQueryGroupComponents<T>(
            Allocator allocator, out NativeArray<ComponentType> enableComponents, out NativeArray<ComponentType> normalComponents)
        {
            GetQueryGroupComponents(ComponentType.ReadOnly<T>(), allocator, out enableComponents, out normalComponents);
        }

        public static void GetQueryGroupComponents(
            ComponentType componentType, Allocator allocator, out NativeArray<ComponentType> enableComponents, out NativeArray<ComponentType> normalComponents)
        {
            var count = TypeManagerEx.GetQueryGroupCount(componentType.TypeIndex);
            var groups = TypeManagerEx.GetQueryGroups(componentType.TypeIndex);
            var enableCount = 0;
            for (var i = 0; i < count; i++)
            {
                if (groups[i].IsEnableable)
                {
                    enableCount++;
                }
            }

            enableComponents = new NativeArray<ComponentType>(enableCount, allocator);
            normalComponents = new NativeArray<ComponentType>(count - enableCount, allocator);
            var enableIndex = 0;
            var normalIndex = 0;
            for (var i = 0; i < count; i++)
            {
                var component = ComponentType.ReadOnly(groups[i]);
                if (groups[i].IsEnableable)
                {
                    enableComponents[enableIndex++] = component;
                }
                else
                {
                    normalComponents[normalIndex++] = component;
                }
            }
        }

    }
}
