// <copyright file="DynamicDictionaryTestEntry.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

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
            get => this.TagField;
            set => this.TagField = value;
        }

        public int Key
        {
            get => this.KeyField;
            set => this.KeyField = value;
        }

        public int Value
        {
            get => this.ValueField;
            set => this.ValueField = value;
        }
    }
}
