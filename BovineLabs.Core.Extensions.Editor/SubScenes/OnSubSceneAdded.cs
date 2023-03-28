// <copyright file="OnSubSceneAdded.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_SUBSCENE
namespace BovineLabs.Core.Editor.SubScenes
{
    using BovineLabs.Core.SubScenes;
    using Unity.Scenes;
    using UnityEditor;
    using UnityEngine;

    /// <summary> Class automatically adds SubSceneLoadConfig to any new SubScene. </summary>
    [InitializeOnLoad]
    public static class OnSubSceneAdded
    {
        static OnSubSceneAdded()
        {
            ObjectFactory.componentWasAdded += HandleComponentAdded;
        }

        private static void HandleComponentAdded(Component comp)
        {
            if (comp is SubScene)
            {
                if (comp.gameObject.GetComponent<SubSceneLoadConfig>() == null)
                {
                    comp.gameObject.AddComponent<SubSceneLoadConfig>();
                }
            }
        }
    }
}
#endif
