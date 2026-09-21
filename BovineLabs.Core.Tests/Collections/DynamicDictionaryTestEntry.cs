namespace BovineLabs.Core.Tests.Collections
{
    using BovineLabs.Core.Collections;

    internal struct DynamicDictionaryTestEntry : IDynamicDictionaryEntry<int, int>
    {
        public uint TagField;
        public int KeyField;
        public int ValueField;

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

        public int Value
        {
            get => ValueField;
            set => ValueField = value;
        }
    }
}
