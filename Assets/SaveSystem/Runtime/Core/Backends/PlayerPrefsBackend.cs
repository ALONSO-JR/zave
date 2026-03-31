using System;
using System.Collections.Generic;
using UnityEngine;

namespace SaveSystemUltimate.Core.Backends
{
    /// <summary>
    /// Guarda los datos serializados en `PlayerPrefs` convirtiendo el arreglo de bytes en base64.
    /// También gestiona un índice especial para poder listar todas las claves de guardado.
    /// Especialmente útil y recomendado en **WebGL** ya que Unity moderno lo maneja de manera nativa
    /// sin necesidad de plugins o scripts JS extra.
    /// </summary>
    public class PlayerPrefsBackend : ISaveBackend
    {
        private const string IndexKey = "SaveSystemUltimate_KeysIndex";

        /// <summary>
        /// Obtiene el índice de claves almacenadas actualmente.
        /// </summary>
        private List<string> GetIndex()
        {
            string json = PlayerPrefs.GetString(IndexKey, "{}");
            // Manejo simplificado usando un envoltorio serializable
            var wrapper = JsonUtility.FromJson<KeysWrapper>(json);
            return wrapper != null && wrapper.keys != null ? wrapper.keys : new List<string>();
        }

        private void SaveIndex(List<string> index)
        {
            var wrapper = new KeysWrapper { keys = index };
            string json = JsonUtility.ToJson(wrapper);
            PlayerPrefs.SetString(IndexKey, json);
            PlayerPrefs.Save();
        }

        private void AddToIndex(string key)
        {
            var index = GetIndex();
            if (!index.Contains(key))
            {
                index.Add(key);
                SaveIndex(index);
            }
        }

        private void RemoveFromIndex(string key)
        {
            var index = GetIndex();
            if (index.Contains(key))
            {
                index.Remove(key);
                SaveIndex(index);
            }
        }

        public void Save(string key, byte[] data)
        {
            if (data == null) return;
            string base64 = Convert.ToBase64String(data);
            PlayerPrefs.SetString(key, base64);
            AddToIndex(key);
            PlayerPrefs.Save();
        }

        public byte[] Load(string key)
        {
            if (PlayerPrefs.HasKey(key))
            {
                string base64 = PlayerPrefs.GetString(key);
                if (string.IsNullOrEmpty(base64)) return null;

                try
                {
                    return Convert.FromBase64String(base64);
                }
                catch (FormatException)
                {
                    Debug.LogWarning($"[PlayerPrefsBackend] Los datos en '{key}' no están en formato Base64. Es posible que estén corruptos.");
                    return null;
                }
            }
            return null;
        }

        public void Delete(string key)
        {
            if (PlayerPrefs.HasKey(key))
            {
                PlayerPrefs.DeleteKey(key);
                RemoveFromIndex(key);
                PlayerPrefs.Save();
            }
        }

        public bool HasKey(string key)
        {
            return PlayerPrefs.HasKey(key) && GetIndex().Contains(key);
        }

        public IEnumerable<string> GetAllKeys()
        {
            return GetIndex();
        }

        public void DeleteAll()
        {
            var keys = GetIndex();
            foreach (var key in keys)
            {
                PlayerPrefs.DeleteKey(key);
            }
            PlayerPrefs.DeleteKey(IndexKey);
            PlayerPrefs.Save();
        }

        [Serializable]
        private class KeysWrapper
        {
            public List<string> keys = new List<string>();
        }
    }
}
