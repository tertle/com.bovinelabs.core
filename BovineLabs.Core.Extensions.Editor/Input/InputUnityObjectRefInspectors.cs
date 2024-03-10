// <copyright file="InputUnityObjectRefInspectors.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_INPUT
namespace BovineLabs.Core.Editor.Input
{
    using BovineLabs.Core.Editor.Inspectors;
    using JetBrains.Annotations;
    using UnityEngine.InputSystem;

    [UsedImplicitly]
    internal class InputActionAssetUnityObjectRefInspector : UnityObjectRefInspector<InputActionAsset>
    {
    }

    [UsedImplicitly]
    internal class InputActionReferenceUnityObjectRefInspector : UnityObjectRefInspector<InputActionReference>
    {
    }
}
#endif
