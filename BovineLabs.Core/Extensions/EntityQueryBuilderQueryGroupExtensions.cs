namespace BovineLabs.Core.Extensions
{
    using BovineLabs.Core.Utility;
    using EntityQueryBuilderAccess = BovineLabs.Core.Internal.EntityQueryBuilderAccess;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    public static unsafe class EntityQueryBuilderQueryGroupExtensions
    {
        public static EntityQueryBuilder WithAnyQueryGroup<T>(this EntityQueryBuilder builder)
        {
            return builder.WithAnyQueryGroup(ComponentType.ReadOnly<T>());
        }

        public static EntityQueryBuilder WithAnyQueryGroup(this EntityQueryBuilder builder, ComponentType type)
        {
            var members = TypeManagerEx.GetQueryGroups(type.TypeIndex);
            var count = TypeManagerEx.GetQueryGroupCount(type.TypeIndex);
            for (var i = 0; i < count; i++)
            {
                builder.WithAny(ComponentType.ReadOnly(members[i]));
            }

            return builder;
        }

        public static EntityQueryBuilder WithNoneQueryGroup<T>(this EntityQueryBuilder builder)
        {
            return builder.WithNoneQueryGroup(ComponentType.ReadOnly<T>());
        }

        public static EntityQueryBuilder WithNoneQueryGroup(this EntityQueryBuilder builder, ComponentType type)
        {
            var members = TypeManagerEx.GetQueryGroups(type.TypeIndex);
            var count = TypeManagerEx.GetQueryGroupCount(type.TypeIndex);
            for (var i = 0; i < count; i++)
            {
                builder.WithNone(ComponentType.ReadOnly(members[i]));
            }

            return builder;
        }

        /// <summary>
        /// Applies the former FilterWriteGroup rules using QueryGroup attributes to the current query description.
        /// Call after its constraints and before Build or AddAdditionalQuery; later constraints are not considered.
        /// </summary>
        public static EntityQueryBuilder WithQueryGroupFilter(this EntityQueryBuilder builder)
        {
            var explicitTypes = new UnsafeList<TypeIndex>(16, Allocator.Temp);
            AddExplicitTypes(EntityQueryBuilderAccess.GetAll(builder), ref explicitTypes);
            AddExplicitTypes(EntityQueryBuilderAccess.GetAny(builder), ref explicitTypes);
            AddExplicitTypes(EntityQueryBuilderAccess.GetNone(builder), ref explicitTypes);
            AddExplicitTypes(EntityQueryBuilderAccess.GetDisabled(builder), ref explicitTypes);
            AddExplicitTypes(EntityQueryBuilderAccess.GetAbsent(builder), ref explicitTypes);
            AddExplicitTypes(EntityQueryBuilderAccess.GetPresent(builder), ref explicitTypes);

            IncludeReadOnlyGroups(EntityQueryBuilderAccess.GetAll(builder), ref explicitTypes);
            IncludeReadOnlyGroups(EntityQueryBuilderAccess.GetAny(builder), ref explicitTypes);
            IncludeReadOnlyGroups(EntityQueryBuilderAccess.GetDisabled(builder), ref explicitTypes);
            IncludeReadOnlyGroups(EntityQueryBuilderAccess.GetPresent(builder), ref explicitTypes);

            ExcludeWritableGroups(EntityQueryBuilderAccess.GetAll(builder), explicitTypes, ref EntityQueryBuilderAccess.GetNone(builder));
            ExcludeWritableGroups(EntityQueryBuilderAccess.GetAny(builder), explicitTypes, ref EntityQueryBuilderAccess.GetNone(builder));
            ExcludeWritableGroups(EntityQueryBuilderAccess.GetDisabled(builder), explicitTypes, ref EntityQueryBuilderAccess.GetNone(builder));
            ExcludeWritableGroups(EntityQueryBuilderAccess.GetPresent(builder), explicitTypes, ref EntityQueryBuilderAccess.GetNone(builder));
            EntityQueryBuilderAccess.Invalidate(builder);
            return builder;
        }

        private static void AddExplicitTypes(UnsafeList<ComponentType> components, ref UnsafeList<TypeIndex> explicitTypes)
        {
            foreach (var component in components)
            {
                if (!explicitTypes.Contains(component.TypeIndex))
                {
                    explicitTypes.Add(component.TypeIndex);
                }
            }
        }

        private static void IncludeReadOnlyGroups(UnsafeList<ComponentType> components, ref UnsafeList<TypeIndex> explicitTypes)
        {
            var pending = new UnsafeList<TypeIndex>(components.Length, Allocator.Temp);
            foreach (var component in components)
            {
                if (component.AccessModeType == ComponentType.AccessMode.ReadOnly)
                {
                    pending.Add(component.TypeIndex);
                }
            }

            // Iteration over a growing work list handles transitive groups and cycles without recursion.
            for (var i = 0; i < pending.Length; i++)
            {
                var members = TypeManagerEx.GetQueryGroups(pending[i]);
                var count = TypeManagerEx.GetQueryGroupCount(pending[i]);
                for (var j = 0; j < count; j++)
                {
                    if (explicitTypes.Contains(members[j]))
                    {
                        continue;
                    }

                    explicitTypes.Add(members[j]);
                    pending.Add(members[j]);
                }
            }
        }

        private static void ExcludeWritableGroups(UnsafeList<ComponentType> components, UnsafeList<TypeIndex> explicitTypes, ref UnsafeList<ComponentType> none)
        {
            foreach (var component in components)
            {
                if (component.AccessModeType != ComponentType.AccessMode.ReadWrite)
                {
                    continue;
                }

                var members = TypeManagerEx.GetQueryGroups(component.TypeIndex);
                var count = TypeManagerEx.GetQueryGroupCount(component.TypeIndex);
                for (var i = 0; i < count; i++)
                {
                    var excluded = ComponentType.ReadOnly(members[i]);
                    if (!explicitTypes.Contains(members[i]) && !none.Contains(excluded))
                    {
                        none.Add(excluded);
                    }
                }
            }
        }
    }
}
