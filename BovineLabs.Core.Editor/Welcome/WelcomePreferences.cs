// <copyright file="WelcomePreferences.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Welcome
{
    using System;
    using BovineLabs.Core.Editor.EditorPreferences;
    using Unity.Serialization.Editor;
    using UnityEngine;

    /// <summary>
    /// Editor preferences for Favourites feature.
    /// </summary>
    [CoreEditorPreference("Welcome")]
    [Serializable]
    public class WelcomePreferences : IEditorPreference
    {
        [SerializeField]
        private bool alwaysShowOnStartup;

        [SerializeField]
        [HideInInspector]
        private bool welcomePopupAlreadyShownOnce;

        public bool AlwaysShowOnStartup
        {
            get => this.alwaysShowOnStartup;
            set => this.alwaysShowOnStartup = value;
        }

        public bool WelcomePopupAlreadyShownOnce
        {
            get => this.welcomePopupAlreadyShownOnce;
            set => this.welcomePopupAlreadyShownOnce = value;
        }

        public static WelcomePreferences Get()
        {
            return UserSettings<WelcomePreferences>.GetOrCreate("Welcome");
        }

        /// <inheritdoc />
        public string[] GetSearchKeywords()
        {
            return IEditorPreference.GetSearchKeywordsFromType(typeof(WelcomePreferences));
        }
    }
}
