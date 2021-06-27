// <copyright file="SaveFile.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Serialization
{
    /// <summary> The save file for the world. </summary>
    public class SaveFile
    {
        /// <summary> Initializes a new instance of the <see cref="SaveFile" /> class. </summary>
        /// <param name="data"> Saved data. </param>
        /// <param name="referencedObjects"> Saved reference objects. </param>
        public SaveFile(byte[] data, object[] referencedObjects)
        {
            this.ReferencedObjects = referencedObjects;
            this.Data = data;
        }

        /// <summary> Gets the world data. </summary>
        public byte[] Data { get; }

        /// <summary> Gets the reference data from the world. </summary>
        public object[] ReferencedObjects { get; }
    }
}