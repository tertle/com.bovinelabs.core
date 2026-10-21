namespace BovineLabs.Core.Extensions
{
    using Unity.Collections;
    using Unity.Entities;

    public static class EntityQueryBuilderExtensions
    {
        public static EntityQueryBuilder WithAll(this EntityQueryBuilder entityQueryBuilder, ComponentType type)
        {
            if (type == default)
            {
                return entityQueryBuilder;
            }

            var list = new FixedList32Bytes<ComponentType>
            {
                new()
                {
                    TypeIndex = type.TypeIndex,
                    AccessModeType = ComponentType.AccessMode.ReadOnly,
                },
            };

            entityQueryBuilder.WithAll(ref list);
            return entityQueryBuilder;
        }

        public static EntityQueryBuilder WithAllRW(this EntityQueryBuilder entityQueryBuilder, ComponentType type)
        {
            if (type == default)
            {
                return entityQueryBuilder;
            }

            var list = new FixedList32Bytes<ComponentType>
            {
                new()
                {
                    TypeIndex = type.TypeIndex,
                    AccessModeType = ComponentType.AccessMode.ReadWrite,
                },
            };

            entityQueryBuilder.WithAll(ref list);
            return entityQueryBuilder;
        }

        public static EntityQueryBuilder WithAny(this EntityQueryBuilder entityQueryBuilder, ComponentType type)
        {
            if (type == default)
            {
                return entityQueryBuilder;
            }

            var list = new FixedList32Bytes<ComponentType>
            {
                new()
                {
                    TypeIndex = type.TypeIndex,
                    AccessModeType = ComponentType.AccessMode.ReadOnly,
                },
            };

            entityQueryBuilder.WithAny(ref list);
            return entityQueryBuilder;
        }

        public static EntityQueryBuilder WithAnyRW(this EntityQueryBuilder entityQueryBuilder, ComponentType type)
        {
            if (type == default)
            {
                return entityQueryBuilder;
            }

            var list = new FixedList32Bytes<ComponentType>
            {
                new()
                {
                    TypeIndex = type.TypeIndex,
                    AccessModeType = ComponentType.AccessMode.ReadWrite,
                },
            };

            entityQueryBuilder.WithAny(ref list);
            return entityQueryBuilder;
        }

        public static EntityQueryBuilder WithNone(this EntityQueryBuilder entityQueryBuilder, ComponentType type)
        {
            if (type == default)
            {
                return entityQueryBuilder;
            }

            var list = new FixedList32Bytes<ComponentType> { type };
            entityQueryBuilder.WithNone(ref list);
            return entityQueryBuilder;
        }

    }
}
