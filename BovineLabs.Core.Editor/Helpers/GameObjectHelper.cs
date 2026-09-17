namespace BovineLabs.Core.Editor.Helpers
{
    using System;
    using System.Reflection;
    using BovineLabs.Core.Extensions;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.Assertions;

    public static class GameObjectHelper
    {
        /// <summary>
        /// Only supports component types declared in an asmdef.
        /// </summary>
        public static void AddAuthoringComponent(GameObject gameObject, Type type)
        {
            AddAuthoringComponent(new[] { gameObject }, type);
        }

        /// <summary>
        /// Only supports component types declared in an asmdef.
        /// </summary>
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
