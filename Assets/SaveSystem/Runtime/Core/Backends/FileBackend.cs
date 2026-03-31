using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SaveSystemUltimate.Core.Backends
{
    /// <summary>
    /// Guarda los datos localmente en un directorio persistente (`Application.persistentDataPath`).
    /// Ideal para Windows, Mac, Android e iOS.
    /// </summary>
    public class FileBackend : ISaveBackend
    {
        private readonly string saveDirectory;
        private const string Extension = ".sav";

        public FileBackend()
        {
            saveDirectory = Path.Combine(Application.persistentDataPath, "Saves");
            EnsureDirectoryExists();
        }

        private void EnsureDirectoryExists()
        {
            if (!Directory.Exists(saveDirectory))
            {
                Directory.CreateDirectory(saveDirectory);
            }
        }

        private string GetFilePath(string key)
        {
            // Filtramos caracteres inválidos de la clave para hacerla un nombre de archivo seguro
            string safeKey = string.Join("_", key.Split(Path.GetInvalidFileNameChars()));
            return Path.Combine(saveDirectory, safeKey + Extension);
        }

        public void Save(string key, byte[] data)
        {
            EnsureDirectoryExists();
            File.WriteAllBytes(GetFilePath(key), data);
        }

        public byte[] Load(string key)
        {
            string path = GetFilePath(key);
            if (File.Exists(path))
            {
                return File.ReadAllBytes(path);
            }
            return null;
        }

        public void Delete(string key)
        {
            string path = GetFilePath(key);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        public bool HasKey(string key)
        {
            return File.Exists(GetFilePath(key));
        }

        public IEnumerable<string> GetAllKeys()
        {
            if (!Directory.Exists(saveDirectory)) return new string[0];

            List<string> keys = new List<string>();
            string[] files = Directory.GetFiles(saveDirectory, "*" + Extension);
            foreach (var file in files)
            {
                keys.Add(Path.GetFileNameWithoutExtension(file));
            }
            return keys;
        }

        public void DeleteAll()
        {
            if (Directory.Exists(saveDirectory))
            {
                Directory.Delete(saveDirectory, true);
                EnsureDirectoryExists();
            }
        }
    }
}
