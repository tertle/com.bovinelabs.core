// <copyright file="OnApplicationFocusBehaviour.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_INPUT
namespace BovineLabs.Core.Input
{
    using UnityEngine;

    internal class OnApplicationFocusBehaviour : MonoBehaviour
    {
        public bool Value { get; private set; } = true;

        public void OnApplicationFocus(bool focus)
        {
            this.Value = focus;
        }
    }
}
#endif
