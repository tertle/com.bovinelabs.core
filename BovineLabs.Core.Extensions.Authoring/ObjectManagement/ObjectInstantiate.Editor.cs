// <copyright file="ObjectInstantiate.Editor.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_OBJECT_DEFINITION
namespace BovineLabs.Core.Authoring.ObjectManagement
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    // I don't usually like having editor code in authoring and prefer it in the editor assembly, however this work requires
    // ExecuteAlways and the GameObject callbacks.
    [ExecuteAlways]
    [InitializeOnLoad]
    public partial class ObjectInstantiate
    {
        private static readonly Dictionary<GameObject, ObjectInstantiate> PreviewInstantiateMap = new();

        private ObjectDefinitionAuthoring? preview;
        private GameObject[] previewChildren = Array.Empty<GameObject>();
        private ObjectDefinition? previousDefinition;

        static ObjectInstantiate()
        {
            Selection.selectionChanged += OnSelectionChanged;
#if !BL_DISABLE_OBJECT_AUTO_INSTANTIATE
            ObjectChangeEvents.changesPublished += ChangesPublished;
#endif
        }

        public static void TryReplace(GameObject newGameObject)
        {
            var objectDefinitionAuthoring = newGameObject.GetComponent<ObjectDefinitionAuthoring>();
            if (objectDefinitionAuthoring == null || objectDefinitionAuthoring.Definition == null)
            {
                return;
            }

            var scene = objectDefinitionAuthoring.gameObject.scene;
            if (!scene.IsValid())
            {
                return;
            }

            var go = new GameObject(newGameObject.name);
            var instance = go.AddComponent<ObjectInstantiate>();
            instance.definition = objectDefinitionAuthoring.Definition;
            go.transform.position = newGameObject.transform.position;

            SceneManager.MoveGameObjectToScene(go, scene);

            if (newGameObject.transform.parent != null)
            {
                go.transform.SetParent(newGameObject.transform.parent, true);
            }

            instance.RebuildPreview();

            DestroyImmediate(newGameObject);

            Selection.SetActiveObjectWithContext(instance, go);
        }

        public void RebuildPreview()
        {
            this.DestroyPreview();
            this.CreatePreview();
        }

        private void CreatePreview()
        {
            if (EditorApplication.isPlaying)
            {
                return;
            }

            if (!this.gameObject.scene.IsValid() || string.IsNullOrEmpty(this.gameObject.scene.path))
            {
                return;
            }

            if (this.definition == null || this.definition.Prefab == null)
            {
                return;
            }

            var prefab = this.definition.Prefab.GetComponent<ObjectDefinitionAuthoring>();
            if (prefab == null)
            {
                return;
            }

            this.previousDefinition = this.definition;

            this.preview = (ObjectDefinitionAuthoring)PrefabUtility.InstantiatePrefab(prefab);
            this.preview.gameObject.hideFlags = HideFlags.HideAndDontSave;

            // Bakers don't like children in subscenes even if hierarchy disabled, so we manually sync position instead of parenting
            this.preview.transform.position = this.transform.position;

            SceneManager.MoveGameObjectToScene(this.preview.gameObject, SceneManager.GetActiveScene());

            this.previewChildren = this.preview.GetComponentsInChildren<Transform>().Select(g => g.gameObject).ToArray();

            foreach (var c in this.previewChildren)
            {
                PreviewInstantiateMap.Add(c.gameObject, this);
            }
        }

        private void DestroyPreview()
        {
            if (this.preview == null)
            {
                return;
            }

            this.previousDefinition = null;

            DestroyImmediate(this.preview.gameObject);

            foreach (var g in this.previewChildren)
            {
                PreviewInstantiateMap.Remove(g);
            }

            this.previewChildren = Array.Empty<GameObject>();
            this.preview = null;
        }

        private static void OnSelectionChanged()
        {
            if (EditorApplication.isPlaying)
            {
                return;
            }

            var selected = Selection.activeGameObject;
            if (selected == null)
            {
                return;
            }

            if (PreviewInstantiateMap.TryGetValue(selected, out var instantiate))
            {
                _ = SetActive(instantiate);
            }
        }

        private static void ChangesPublished(ref ObjectChangeEventStream stream)
        {
            for (var i = 0; i < stream.length; ++i)
            {
                var type = stream.GetEventType(i);
                if (type != ObjectChangeKind.CreateGameObjectHierarchy)
                {
                    continue;
                }

                stream.GetCreateGameObjectHierarchyEvent(i, out var createGameObjectHierarchyEvent);
                var newGameObject = (GameObject)EditorUtility.InstanceIDToObject(createGameObjectHierarchyEvent.instanceId);
                TryReplace(newGameObject);
            }
        }

        private static async Task SetActive(ObjectInstantiate objectInstantiate)
        {
            // We need to delay
            await Task.Yield(); // don't like how this causes a flicker
            Selection.SetActiveObjectWithContext(objectInstantiate, objectInstantiate.gameObject);
        }

        private void OnEnable()
        {
            this.CreatePreview();
        }

        private void OnDisable()
        {
            this.DestroyPreview();
        }

        private void OnDestroy()
        {
            this.DestroyPreview();
        }

        private void Update()
        {
            if (EditorApplication.isPlaying)
            {
                return;
            }

            if (this.previousDefinition != this.definition)
            {
                this.RebuildPreview();
            }

            if (this.preview == null)
            {
                return;
            }

            this.preview.transform.position = this.transform.position;
            this.preview.transform.rotation = this.transform.rotation;
            this.preview.transform.localScale = this.transform.localScale;
        }
    }
}
#endif
