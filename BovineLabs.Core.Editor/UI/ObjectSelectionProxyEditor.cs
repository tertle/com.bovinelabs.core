// <copyright file="ObjectSelectionProxyEditor.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.UI
{
    using BovineLabs.Core.Editor.Internal;
    using UnityEditor;
    using UnityEngine.UIElements;

    [CustomEditor(typeof(ObjectSelectionProxy))]
    public class ObjectSelectionProxyEditor : Editor
    {
        public sealed override VisualElement CreateInspectorGUI()
        {
            var obj = (ObjectSelectionProxy)this.target;

            if (obj.Obj == null)
            {
                return base.CreateInspectorGUI();
            }

            return PropertyInspector.Make(obj.Obj);
        }

        protected override void OnHeaderGUI()
        {
        }
    }
}
