// <copyright file="GameObjectHelper.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Helpers
{
    using System;
    using System.Reflection;
    using BovineLabs.Core.Extensions;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.Assertions;

    /// <summary> Helpers for <see cref="GameObject" />. </summary>
    public static class GameObjectHelper
    {
        /// <summary>
        /// Add a component to of GameObject.
        /// This is intended for use on entity components generated via <see cref="GenerateAuthoringComponentAttribute" />.
        /// </summary>
        /// <remarks> Note this only works on types in an asmdef. </remarks>
        /// <param name="gameObject"> GameObject to add the component type to. </param>
        /// <param name="type"> The component type to add. </param>
        public static void AddAuthoringComponent(GameObject gameObject, Type type)
        {
            AddAuthoringComponent(new[] { gameObject }, type);
        }

        /// <summary>
        /// Add a component to a set of GameObjects.
        /// This is intended for use on entity components generated via <see cref="GenerateAuthoringComponentAttribute" />.
        /// </summary>
        /// <remarks> Note this only works on types in an asmdef. </remarks>
        /// <param name="gameObjects"> GameObjects to add the component type to. </param>
        /// <param name="type"> The component type to add. </param>
        public static void AddAuthoringComponent(GameObject[] gameObjects, Type type)
        {
            var executeMethod = typeof(EditorApplication).GetMethod("ExecuteMenuItemOnGameObjects", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(executeMethod, "ExecuteMenuItemOnGameObjects has been removed");

            var assemblyName = type.Assembly.GetName().Name;
            var componentName = type.Name.ToSentence();
            var file = $"Component/Scripts/{assemblyName}/{componentName}";

            executeMethod.Invoke(null, new object[] { file, gameObjects });
        }
    }
}
