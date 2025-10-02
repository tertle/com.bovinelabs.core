// <copyright file="ObjectGroup.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_OBJECT_DEFINITION
namespace BovineLabs.Core.Authoring.ObjectManagement
{
    using System;
    using System.Collections.Generic;
    using BovineLabs.Core.ObjectManagement;
    using BovineLabs.Core.PropertyDrawers;
    using UnityEngine;

    [AutoRef("ObjectManagementSettings", "objectGroups", nameof(ObjectGroup), "ObjectGroups")]
    public sealed class ObjectGroup : ScriptableObject, IUID
    {
        [InspectorReadOnly]
        [SerializeField]
        private short id;

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

        public int AutoGroups => this.autoGroups;

        /// <inheritdoc />
        int IUID.ID
        {
            get => this.id;
            set => this.id = (short)value;
        }

        public static implicit operator GroupId(ObjectGroup group)
        {
            return group == null ? default : new GroupId(group.id);
        }

        public IEnumerable<ObjectDefinition> GetAllDefinitions()
        {
            var uniqueGroups = new HashSet<ObjectGroup>();
            var uniqueDefinitions = new HashSet<ObjectDefinition>();
            this.GetAllDefinitionsInternal(uniqueGroups, uniqueDefinitions);
            return uniqueDefinitions;
        }

        public IEnumerable<ScriptableObject> GetAllDependencies()
        {
            var uniqueObjects = new HashSet<ScriptableObject>();
            this.GetAllDependenciesInternal(uniqueObjects);
            return uniqueObjects;
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

        private void GetAllDependenciesInternal(HashSet<ScriptableObject> dependencies)
        {
            foreach (var group in this.groups)
            {
                if (group == null)
                {
                    continue;
                }

                // Avoid infinite loops by only processing object groups once
                if (dependencies.Add(group))
                {
                    group.GetAllDependenciesInternal(dependencies);
                }
            }

            foreach (var definition in this.definitions)
            {
                if (definition != null)
                {
                    dependencies.Add(definition);
                }
            }

            foreach (var definition in this.excludeDefinitions)
            {
                if (definition != null)
                {
                    dependencies.Add(definition);
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
                if (dependencies.Add(group))
                {
                    group.GetAllDependenciesInternal(dependencies);
                }
            }
        }
    }
}
#endif
