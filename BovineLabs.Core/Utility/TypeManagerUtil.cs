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

        public static NativeArray<ComponentType> GetWriteGroupComponents<T>(Allocator allocator)
        {
            return GetWriteGroupComponents(ComponentType.ReadOnly<T>(), allocator);
        }

        public static NativeArray<ComponentType> GetWriteGroupComponents(ComponentType componentType, Allocator allocator)
        {
            var typeInfo = TypeManager.GetTypeInfo(componentType.TypeIndex);
            var writeGroups = TypeManager.GetWriteGroups(typeInfo);
            var writeGroupCount = typeInfo.WriteGroupCount;

            var componentTypes = new NativeArray<ComponentType>(typeInfo.WriteGroupCount, allocator);
            for (var i = 0; i < writeGroupCount; i++)
            {
                componentTypes[i] = GetWriteGroupReadOnlyComponentType(writeGroups, i);
            }

            return componentTypes;
        }

        public static void GetWriteGroupComponents<T>(
            Allocator allocator, out NativeArray<ComponentType> enableComponents, out NativeArray<ComponentType> normalComponents)
        {
            GetWriteGroupComponents(ComponentType.ReadOnly<T>(), allocator, out enableComponents, out normalComponents);
        }

        public static void GetWriteGroupComponents(
            ComponentType componentType, Allocator allocator, out NativeArray<ComponentType> enableComponents, out NativeArray<ComponentType> normalComponents)
        {
            var typeInfo = TypeManager.GetTypeInfo(componentType.TypeIndex);
            var writeGroups = TypeManager.GetWriteGroups(typeInfo);
            var writeGroupCount = typeInfo.WriteGroupCount;

            var enableComponentCount = 0;

            for (var i = 0; i < writeGroupCount; i++)
            {
                ref readonly var t = ref TypeManager.GetTypeInfo(writeGroups[i]);
                if (t.EnableableType)
                {
                    enableComponentCount++;
                }
            }

            enableComponents = new NativeArray<ComponentType>(enableComponentCount, allocator);
            normalComponents = new NativeArray<ComponentType>(typeInfo.WriteGroupCount - enableComponentCount, allocator);

            var enableIndex = 0;
            var normalIndex = 0;

            for (var i = 0; i < writeGroupCount; i++)
            {
                ref readonly var t = ref TypeManager.GetTypeInfo(writeGroups[i]);
                if (t.EnableableType)
                {
                    enableComponents[enableIndex++] = GetWriteGroupReadOnlyComponentType(writeGroups, i);
                }
                else
                {
                    normalComponents[normalIndex++] = GetWriteGroupReadOnlyComponentType(writeGroups, i);
                }
            }
        }

        // Copied from EntityQueryManager
        private static ComponentType GetWriteGroupReadOnlyComponentType(TypeIndex* writeGroupTypes, int i)
        {
            // Need to get "Clean" TypeIndex from Type. Since TypeInfo.TypeIndex is not actually the index of the
            // type. (It includes other flags.) What is stored in WriteGroups is the actual index of the type.
            ref readonly var excludedType = ref TypeManager.GetTypeInfo(writeGroupTypes[i]);
            return ComponentType.ReadOnly(excludedType.TypeIndex);
        }
    }
}
