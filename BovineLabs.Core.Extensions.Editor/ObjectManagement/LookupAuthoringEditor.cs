// <copyright file="LookupAuthoringEditor.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_OBJECT_DEFINITION
namespace BovineLabs.Core.Editor.ObjectManagement
{
    using BovineLabs.Core.Authoring.ObjectManagement;
    using BovineLabs.Core.Editor.Inspectors;
    using UnityEditor;

    [CustomEditor(typeof(ILookupAuthoring<,>), true)]
    public class LookupAuthoringEditor : PrefabElementEditor
    {
    }

    [CustomEditor(typeof(ILookupMultiAuthoring<,>), true)]
    public class LookupMultiAuthoringEditor : PrefabElementEditor
    {
    }
}
#endif
