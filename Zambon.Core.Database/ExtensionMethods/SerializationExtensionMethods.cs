﻿using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Zambon.Core.Database.ChangeTracker;

namespace Zambon.Core.Database.ExtensionMethods
{
    /// <summary>
    /// Extension methods to serialize and deserialize objects from array of bytes.
    /// </summary>
    public static class SerializationExtensionMethods
    {

        /// <summary>
        /// Deserialize an array of stored keys.
        /// </summary>
        /// <param name="bytes">The bytes array to deserialize.</param>
        /// <returns>Returns an array of StoreKey.</returns>
        public static StoreKey[] DeserializeStoreKeys(this byte[] bytes)
        {
            if (bytes == null)
                return new StoreKey[0];

            using (var ms = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                ms.Write(bytes, 0, bytes.Length);
                ms.Position = 0;

                return formatter.Deserialize(ms) as StoreKey[];
            }
        }

        /// <summary>
        /// Serialize an array of stored keys.
        /// </summary>
        /// <param name="keys">The StoreKey array to serialize.</param>
        /// <returns>Returns an array of bytes.</returns>
        public static byte[] SerializeStoreKeys(this StoreKey[] keys)
        {
            using (var ms = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(ms, keys);

                return ms.ToArray();
            }
        }


        /// <summary>
        /// Deserialize a dictionary of the object's changed properties.
        /// </summary>
        /// <param name="bytes">The bytes array to deserialize.</param>
        /// <returns>Returns a dictionary of string (property name) and object (value).</returns>
        public static Dictionary<string, object> DeserializeStoredObject(this byte[] bytes)
        {
            if (bytes == null)
                return new Dictionary<string,object>();

            using (var ms = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                ms.Write(bytes, 0, bytes.Length);
                ms.Position = 0;

                return formatter.Deserialize(ms) as Dictionary<string, object>;
            }
        }

        /// <summary>
        /// Serialize a dictionary of the object's changed properties.
        /// </summary>
        /// <param name="dictionary">The dictionary to serialize.</param>
        /// <returns>Returns an array of bytes.</returns>
        public static byte[] SerializeStoredObject(this Dictionary<string, object> dictionary)
        {
            using (var ms = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(ms, dictionary);

                return ms.ToArray();
            }
        }

    }
}