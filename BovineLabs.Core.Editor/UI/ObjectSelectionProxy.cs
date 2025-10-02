// <copyright file="ObjectSelectionProxy.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.UI
{
    using UnityEditor;
    using UnityEngine;

    public class ObjectSelectionProxy : ScriptableObject, ISerializationCallbackReceiver
    {
        // [SerializeReference]
        private object? obj;

        public object? Obj
        {
            get => this.obj;
            set => this.obj = value;
        }

        public static ObjectSelectionProxy CreateInstance(object obj)
        {
            var proxy = CreateInstance<ObjectSelectionProxy>();
            proxy.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor | HideFlags.NotEditable;

            proxy.Obj = obj;

            var undoGroup = Undo.GetCurrentGroup();
            Undo.RegisterCreatedObjectUndo(proxy, $"Create {nameof(ObjectSelectionProxy)}({obj})");
            Undo.CollapseUndoOperations(undoGroup);

            return proxy;
        }

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
        }
    }
}
