// <copyright file="IUserPrefs.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Serialization
{
    /// <summary> The <see cref="UserPrefs" /> implementation interface. </summary>
    internal interface IUserPrefs
    {
        /// <summary> Get a string value. </summary>
        /// <param name="key"> The key. </param>
        /// <param name="defaultValue"> Default value to return if key not found. </param>
        /// <returns> The value. </returns>
        string GetString(string key, string defaultValue);

        /// <summary> Get a bool value. </summary>
        /// <param name="key"> The key. </param>
        /// <param name="defaultValue"> Default value to return if key not found. </param>
        /// <returns> The value. </returns>
        bool GetBool(string key, bool defaultValue);

        /// <summary> Get a int value. </summary>
        /// <param name="key"> The key. </param>
        /// <param name="defaultValue"> Default value to return if key not found. </param>
        /// <returns> The value. </returns>
        int GetInt(string key, int defaultValue);

        /// <summary> Get a float value. </summary>
        /// <param name="key"> The key. </param>
        /// <param name="defaultValue"> Default value to return if key not found. </param>
        /// <returns> The value. </returns>
        float GetFloat(string key, float defaultValue);

        /// <summary> Set a string value. </summary>
        /// <param name="key"> The key. </param>
        /// <param name="value"> The value. </param>
        void SetString(string key, string value);

        /// <summary> Set a bool value. </summary>
        /// <param name="key"> The key. </param>
        /// <param name="value"> The value. </param>
        void SetBool(string key, bool value);

        /// <summary> Set an int value. </summary>
        /// <param name="key"> The key. </param>
        /// <param name="value"> The value. </param>
        void SetInt(string key, int value);

        /// <summary> Set a float value. </summary>
        /// <param name="key"> The key. </param>
        /// <param name="value"> The value. </param>
        void SetFloat(string key, float value);

        /// <summary> Deletes a key. </summary>
        /// <param name="key"> The key to delete. </param>
        void DeleteKey(string key);
    }
}
