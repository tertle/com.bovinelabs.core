// <copyright file="UserPrefs.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Serialization
{
    using System;
    using UnityEngine;

    /// <summary> Custom preference implementation. </summary>
    public static class UserPrefs
    {
#if UNITY_EDITOR
        // Unity editor is already pre-initialized with player prefs for the sake of editor tooling
        private static readonly IUserPrefs Impl = new PlayerPrefsUserPrefs();
#else
        private static IUserPrefs Impl;
#endif

        /// <summary> Get a string value. </summary>
        /// <param name="key"> The key. </param>
        /// <param name="defaultValue"> Default value to return if key not found. </param>
        /// <returns> The value. </returns>
        public static string GetString(string key, string defaultValue)
        {
            return Impl.GetString(key, defaultValue);
        }

        /// <summary> Get a bool value. </summary>
        /// <param name="key"> The key. </param>
        /// <param name="defaultValue"> Default value to return if key not found. </param>
        /// <returns> The value. </returns>
        public static bool GetBool(string key, bool defaultValue)
        {
            return Impl.GetBool(key, defaultValue);
        }

        /// <summary> Get a int value. </summary>
        /// <param name="key"> The key. </param>
        /// <param name="defaultValue"> Default value to return if key not found. </param>
        /// <returns> The value. </returns>
        public static int GetInt(string key, int defaultValue)
        {
            return Impl.GetInt(key, defaultValue);
        }

        /// <summary> Get a float value. </summary>
        /// <param name="key"> The key. </param>
        /// <param name="defaultValue"> Default value to return if key not found. </param>
        /// <returns> The value. </returns>
        public static float GetFloat(string key, float defaultValue)
        {
            return Impl.GetFloat(key, defaultValue);
        }

        /// <summary> Set a string value. </summary>
        /// <param name="key"> The key. </param>
        /// <param name="value"> The value. </param>
        /// <exception cref="ArgumentException"> Throw when an invalid string is found in the key. </exception>
        public static void SetString(string key, string value)
        {
            Impl.SetString(key, value);
        }

        /// <summary> Set a bool value. </summary>
        /// <param name="key"> The key. </param>
        /// <param name="value"> The value. </param>
        /// <exception cref="ArgumentException"> Throw when an invalid string is found in the key. </exception>
        public static void SetBool(string key, bool value)
        {
            Impl.SetBool(key, value);
        }

        /// <summary> Set an int value. </summary>
        /// <param name="key"> The key. </param>
        /// <param name="value"> The value. </param>
        public static void SetInt(string key, int value)
        {
            Impl.SetInt(key, value);
        }

        /// <summary> Set a float value. </summary>
        /// <param name="key"> The key. </param>
        /// <param name="value"> The value. </param>
        public static void SetFloat(string key, float value)
        {
            Impl.SetFloat(key, value);
        }

        /// <summary> Deletes a key. </summary>
        /// <param name="key"> The key to delete. </param>
        public static void DeleteKey(string key)
        {
            Impl.DeleteKey(key);
        }

        /// <summary> Initialize the user prefs for the current platform. </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
#if UNITY_EDITOR
            // Unity editor is already pre-initialized with player prefs for the sake of editor tooling
#elif UNITY_STANDALONE
            Impl = new PlayerPrefsUserPrefs();
#else
            throw new NotImplementedException("Current platform not setup for UserPrefs");
#endif
        }
    }
}