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
        private static readonly IUserPrefs impl = new PlayerPrefsUserPrefs();
#else
        private static IUserPrefs impl;
#endif

        /// <summary> Initialize the user prefs for the current platform. </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
#if UNITY_EDITOR
            // Unity editor is already pre-initialized with player prefs for the sake of editor tooling
#elif UNITY_STANDALONE
            impl = new PlayerPrefsUserPrefs();
#else
            throw new NotImplementedException("Current platform not setup for UserPrefs");
#endif
        }

        /// <summary> Get a string value. </summary>
        /// <param name="key"> The key. </param>
        /// <param name="defaultValue"> Default value to return if key not found. </param>
        /// <returns> The value. </returns>
        public static string GetString(string key, string defaultValue)
        {
            return impl.GetString(key, defaultValue);
        }

        /// <summary> Get a bool value. </summary>
        /// <param name="key"> The key. </param>
        /// <param name="defaultValue"> Default value to return if key not found. </param>
        /// <returns> The value. </returns>
        public static bool GetBool(string key, bool defaultValue)
        {
            return impl.GetBool(key, defaultValue);
        }

        /// <summary> Get a int value. </summary>
        /// <param name="key"> The key. </param>
        /// <param name="defaultValue"> Default value to return if key not found. </param>
        /// <returns> The value. </returns>
        public static int GetInt(string key, int defaultValue)
        {
            return impl.GetInt(key, defaultValue);
        }

        /// <summary> Get a float value. </summary>
        /// <param name="key"> The key. </param>
        /// <param name="defaultValue"> Default value to return if key not found. </param>
        /// <returns> The value. </returns>
        public static float GetFloat(string key, float defaultValue)
        {
            return impl.GetFloat(key, defaultValue);
        }

        /// <summary> Set a string value. </summary>
        /// <param name="key"> The key. </param>
        /// <param name="value"> The value. </param>
        /// <exception cref="ArgumentException"> Throw when an invalid string is found in the key. </exception>
        public static void SetString(string key, string value)
        {
            impl.SetString(key, value);
        }

        /// <summary> Set a bool value. </summary>
        /// <param name="key"> The key. </param>
        /// <param name="value"> The value. </param>
        /// <exception cref="ArgumentException"> Throw when an invalid string is found in the key. </exception>
        public static void SetBool(string key, bool value)
        {
            impl.SetBool(key, value);
        }

        /// <summary> Set an int value. </summary>
        /// <param name="key"> The key. </param>
        /// <param name="value"> The value. </param>
        public static void SetInt(string key, int value)
        {
            impl.SetInt(key, value);
        }

        /// <summary> Set a float value. </summary>
        /// <param name="key"> The key. </param>
        /// <param name="value"> The value. </param>
        public static void SetFloat(string key, float value)
        {
            impl.SetFloat(key, value);
        }

        /// <summary> Deletes a key. </summary>
        /// <param name="key"> The key to delete. </param>
        public static void DeleteKey(string key)
        {
            impl.DeleteKey(key);
        }
    }

    internal class PlayerPrefsUserPrefs : IUserPrefs
    {
        public string GetString(string key, string defaultValue)
        {
            return PlayerPrefs.GetString(key, defaultValue);
        }

        public bool GetBool(string key, bool defaultValue)
        {
            return PlayerPrefs.GetInt(key, defaultValue ? 1 : 0) != 0;
        }

        public int GetInt(string key, int defaultValue)
        {
            return PlayerPrefs.GetInt(key, defaultValue);
        }

        public float GetFloat(string key, float defaultValue)
        {
            return PlayerPrefs.GetFloat(key, defaultValue);
        }

        public void SetString(string key, string value)
        {
            PlayerPrefs.SetString(key, value);
        }

        public void SetBool(string key, bool value)
        {
            PlayerPrefs.SetInt(key, value ? 1 : 0);
        }

        public void SetInt(string key, int value)
        {
            PlayerPrefs.SetInt(key, value);
        }

        public void SetFloat(string key, float value)
        {
            PlayerPrefs.SetFloat(key, value);
        }

        public void DeleteKey(string key)
        {
            PlayerPrefs.DeleteKey(key);
        }
    }
}