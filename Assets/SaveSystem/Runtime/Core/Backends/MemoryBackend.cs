using System;
using System.Collections.Generic;
using UnityEngine;

namespace SaveSystemUltimate.Core.Backends
{
    /// <summary>
    /// Guarda los datos temporalmente en la memoria RAM (en un Dictionary).
    /// Ideal para pruebas o estados de sesión temporales.
    /// </summary>
    public class MemoryBackend : ISaveBackend
    {
        private readonly Dictionary<string, byte[]> storage = new Dictionary<string, byte[]>();

        public void Save(string key, byte[] data)
        {
            if (data == null) return;
            // Guardar una copia para evitar mutaciones externas
            byte[] copy = new byte[data.Length];
            Array.Copy(data, copy, data.Length);
            storage[key] = copy;
        }

        public byte[] Load(string key)
        {
            if (storage.TryGetValue(key, out byte[] data))
            {
                // Retornar una copia
                byte[] copy = new byte[data.Length];
                Array.Copy(data, copy, data.Length);
                return copy;
            }
            return null;
        }

        public void Delete(string key)
        {
            if (storage.ContainsKey(key))
            {
                storage.Remove(key);
            }
        }

        public bool HasKey(string key)
        {
            return storage.ContainsKey(key);
        }

        public IEnumerable<string> GetAllKeys()
        {
            return new List<string>(storage.Keys);
        }

        public void DeleteAll()
        {
            storage.Clear();
        }
    }
}
