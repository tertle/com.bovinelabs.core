// <copyright file="ObjectDefinition.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_OBJECT_DEFINITION
namespace BovineLabs.Core.Authoring.ObjectManagement
{
    using BovineLabs.Core.ObjectManagement;
    using BovineLabs.Core.PropertyDrawers;
    using JetBrains.Annotations;
    using UnityEngine;

    /// <summary>
    /// ObjectDefinition provides an auto generated map of an automatically managed, auto incremented and branch safe key to an entity.
    /// This mapping is stored in <see cref="ObjectDefinitionRegistry" /> where the key is the index in the dynamic buffer.
    /// It provides a way to give high definition <see cref="ObjectCategories" /> which can auto place into <see cref="ObjectGroup" /> for you.
    /// </summary>
    [AutoRef("ObjectManagementSettings", "objectDefinitions")]
    public class ObjectDefinition : ScriptableObject, IUID
    {
        [InspectorReadOnly]
        [SerializeField]
        private int id;

        [SerializeField]
        private string friendlyName = string.Empty;

        [SerializeField]
        [UsedImplicitly]
        private string description = string.Empty;

        [ObjectCategories]
        [SerializeField]
        private int categories;

        [SerializeField]
        private GameObject? prefab;

        public GameObject? Prefab
        {
            get => this.prefab;
            internal set => this.prefab = value;
        }

        public int ID => this.id;

        public ObjectCategory Categories => new() { Value = (uint)this.categories };

        public string FriendlyName => string.IsNullOrWhiteSpace(this.friendlyName) ? this.name : this.friendlyName;

        public string Description => this.description;

        int IUID.ID
        {
            get => this.id;
            set => this.id = value;
        }

        public static implicit operator int(ObjectDefinition? definition)
        {
            return definition != null ? definition.id : default;
        }

        public static implicit operator ObjectId(ObjectDefinition? definition)
        {
            // TODO get the mod
            return definition == null ? default : new ObjectId(0, definition.id);
        }
    }
}
#endif
