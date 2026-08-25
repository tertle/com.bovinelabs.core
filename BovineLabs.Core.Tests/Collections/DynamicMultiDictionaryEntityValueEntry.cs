// <copyright file="DynamicMultiDictionaryEntityValueEntry.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Collections
{
    using BovineLabs.Core.Collections;
    using Unity.Entities;

    internal struct DynamicMultiDictionaryEntityValueEntry : IDynamicMultiDictionaryEntry<int, Entity>
    {
        public uint TagField;
        public int KeyField;
        public Entity ValueField;

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

        public Entity Value
        {
            get => this.ValueField;
            set => this.ValueField = value;
        }
    }
}
