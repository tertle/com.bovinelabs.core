// <copyright file="ObjectWindowReloadTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Windows
{
    using System;
    using System.Collections;
    using System.Linq;
    using BovineLabs.Core.Editor.Windows.Favourites;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.TestTools;
    using UnityEngine.UIElements;
    using Object = UnityEngine.Object;

    public class ObjectWindowReloadTests
    {
        // The test runner restores fixture fields after a domain reload, but not coroutine locals.
        [SerializeField]
        private FavouritesWindow window;

        [SerializeField]
        private Object[] previousSelection;

        [UnityTest]
        public IEnumerator Favourites_RebuildsServicesAndSelectionCallbacksAfterDomainReload()
        {
            this.previousSelection = Selection.objects;
            Selection.activeObject = AssetDatabase.FindAssets("t:MonoScript", new[] { "Packages/com.bovinelabs.core" })
                .Select(guid => AssetDatabase.LoadAssetAtPath<MonoScript>(AssetDatabase.GUIDToAssetPath(guid)))
                .First(script => script != null && !FavouritesService.Instance.IsFavourite(script));

            this.window = ScriptableObject.CreateInstance<FavouritesWindow>();
            this.window.ShowUtility();
            this.window.CreateGUI();

            EditorUtility.RequestScriptReload();
            yield return new WaitForDomainReload();

            Assert.IsNotNull(this.window);
            Assert.IsNotNull(Selection.activeObject);
            this.window.CreateGUI();
            this.window.CreateGUI();

            var addSelection = this.window.rootVisualElement.Query<Button>().ToList().Single(button => button.text == "Add Selection");
            Assert.IsTrue(addSelection.enabledSelf);

            Selection.objects = Array.Empty<Object>();
            yield return null;

            Assert.IsFalse(addSelection.enabledSelf, "The rebuilt window must still respond to selection changes.");
        }

        [TearDown]
        public void TearDown()
        {
            if (this.window != null)
            {
                this.window.Close();
            }

            if (this.previousSelection != null)
            {
                Selection.objects = this.previousSelection;
            }
        }
    }
}
