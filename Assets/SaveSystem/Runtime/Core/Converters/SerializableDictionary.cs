using System;
using System.Collections.Generic;
using UnityEngine;

namespace SaveSystemUltimate.Core
{
    /// <summary>
    /// Un diccionario serializable para Unity que permite usar JsonUtility con colecciones tipo Dictionary.
    /// Funciona dividiendo el diccionario en dos listas (claves y valores) antes de la serialización,
    /// y reconstruyendo el diccionario internamente después de la deserialización.
    /// </summary>
    /// <typeparam name="TKey">Tipo de la clave (debe ser serializable por Unity).</typeparam>
    /// <typeparam name="TValue">Tipo del valor (debe ser serializable por Unity).</typeparam>
    [Serializable]
    public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        [SerializeField]
        private List<TKey> keys = new List<TKey>();

        [SerializeField]
        private List<TValue> values = new List<TValue>();

        /// <summary>
        /// Guarda el diccionario en las listas antes de la serialización.
        /// </summary>
        public void OnBeforeSerialize()
        {
            keys.Clear();
            values.Clear();

            foreach (KeyValuePair<TKey, TValue> pair in this)
            {
                keys.Add(pair.Key);
                values.Add(pair.Value);
            }
        }

        /// <summary>
        /// Reconstruye el diccionario a partir de las listas después de la deserialización.
        /// </summary>
        public void OnAfterDeserialize()
        {
            this.Clear();

            if (keys.Count != values.Count)
            {
                Debug.LogError($"[SerializableDictionary] La cantidad de claves ({keys.Count}) y valores ({values.Count}) no coincide después de deserializar. Los datos podrían estar corruptos.");
                return;
            }

            for (int i = 0; i < keys.Count; i++)
            {
                if (this.ContainsKey(keys[i]))
                {
                    Debug.LogError($"[SerializableDictionary] Se encontró una clave duplicada al deserializar: {keys[i]}. Se ignorará el duplicado.");
                    continue;
                }
                this.Add(keys[i], values[i]);
            }
        }
    }
}
