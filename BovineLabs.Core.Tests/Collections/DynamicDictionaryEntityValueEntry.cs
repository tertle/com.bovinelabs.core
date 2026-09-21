namespace BovineLabs.Core.Tests.Collections
{
    using BovineLabs.Core.Collections;
    using Unity.Entities;

    internal struct DynamicDictionaryEntityValueEntry : IDynamicDictionaryEntry<int, Entity>
    {
        public uint TagField;
        public int KeyField;
        public Entity ValueField;

        public uint Tag
        {
            get => TagField;
            set => TagField = value;
        }

        public int Key
        {
            get => KeyField;
            set => KeyField = value;
        }

        public Entity Value
        {
            get => ValueField;
            set => ValueField = value;
        }
    }
}
