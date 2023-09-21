// <copyright file="ObjectGroup.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_OBJECT_DEFINITION
namespace BovineLabs.Core.Authoring.ObjectManagement
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using BovineLabs.Core.ObjectManagement;
    using UnityEngine;

    [UIDManager("ObjectManagementSettings", "objectGroups")]
    [AssetCreator("object.groups", "Assets/Configs/Groups", "Group.asset")]
    public class ObjectGroup : ScriptableObject, IUID
    {
        [HideInInspector] // So the field is not editable even in debug mode.
        [SerializeField]
        private GroupId id;

        [SerializeField]
        [ObjectCategories]
        private int autoGroups;

        [SerializeField]
        private ObjectGroup[] groups = Array.Empty<ObjectGroup>();

        [SerializeField]
        private ObjectDefinition[] definitions = Array.Empty<ObjectDefinition>();

        [SerializeField]
        private ObjectGroup[] excludeGroups = Array.Empty<ObjectGroup>();

        [SerializeField]
        private ObjectDefinition[] excludeDefinitions = Array.Empty<ObjectDefinition>();

        /// <inheritdoc />
        int IUID.ID
        {
            get => this.id;
            set => this.id = new GroupId { ID = (short)value };
        }

        public int AutoGroups => this.autoGroups;

        public static implicit operator GroupId(ObjectGroup group)
        {
            return group == null ? default : group.id;
        }

        public IEnumerable<ObjectDefinition> GetAllDefinitions()
        {
            var uniqueGroups = new HashSet<ObjectGroup>();
            var uniqueDefinitions = new HashSet<ObjectDefinition>();
            this.GetAllDefinitionsInternal(uniqueGroups, uniqueDefinitions);
            return uniqueDefinitions;
        }

        private void GetAllDefinitionsInternal(ISet<ObjectGroup> allGroups, ISet<ObjectDefinition> uniqueDefinitions)
        {
            foreach (var group in this.groups)
            {
                if (group == null)
                {
                    continue;
                }

                // Avoid infinite loops by only processing object groups once
                if (allGroups.Add(group))
                {
                    group.GetAllDefinitionsInternal(allGroups, uniqueDefinitions);
                }
            }

            foreach (var definition in this.definitions)
            {
                if (definition != null)
                {
                    uniqueDefinitions.Add(definition);
                }
            }

            var excludeUnique = new HashSet<ObjectGroup> { this };
            var excludeUniqueDefinitions = new HashSet<ObjectDefinition>();

            foreach (var definition in this.excludeDefinitions)
            {
                if (definition != null)
                {
                    excludeUniqueDefinitions.Add(definition);
                }
            }

            // Get all excludes
            foreach (var group in this.excludeGroups)
            {
                if (group == null)
                {
                    continue;
                }

                // Avoid infinite loops by only processing object groups once
                if (excludeUnique.Add(group))
                {
                    group.GetAllDefinitionsInternal(excludeUnique, excludeUniqueDefinitions);
                }
            }

            foreach (var exclude in excludeUniqueDefinitions)
            {
                uniqueDefinitions.Remove(exclude);
            }
        }
    }
}
#endif
