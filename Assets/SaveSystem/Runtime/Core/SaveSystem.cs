using System;
using System.Text;
using UnityEngine;
using SaveSystemUltimate.Core.Backends;
using SaveSystemUltimate.Core.Encryption;
using SaveSystemUltimate.Core.Compression;
using SaveSystemUltimate.Core.Migration;
using SaveSystemUltimate.Core.Converters;
using System.Collections;
using System.Collections.Generic;

namespace SaveSystemUltimate.Core
{
    /// <summary>
    /// La clase estática principal para interactuar con SaveSystem Ultimate.
    /// Proporciona los métodos para guardar, cargar y gestionar backends.
    /// </summary>
    public static class SaveSystem
    {
        private static ISaveBackend currentBackend;
        private static string globalEncryptionPassword = "";

        // Máscaras de bits para los metadatos (4 bytes de cabecera)
        private const byte FLAG_ENCRYPTED = 1 << 0;
        private const byte FLAG_COMPRESSED = 1 << 1;
        // Metadato versión 1 por defecto (00)

        static SaveSystem()
        {
            // Backend por defecto (Automático: WebGL usa PlayerPrefs, el resto usa FileBackend)
#if UNITY_WEBGL && !UNITY_EDITOR
            currentBackend = new PlayerPrefsBackend();
#else
            currentBackend = new FileBackend();
#endif
        }

        /// <summary>
        /// Cambia el backend de almacenamiento activo a nivel global.
        /// </summary>
        public static void SetBackend(ISaveBackend backend)
        {
            currentBackend = backend ?? throw new ArgumentNullException(nameof(backend));
        }

        /// <summary>
        /// Define la contraseña global para encriptar/desencriptar archivos (AES).
        /// </summary>
        public static void SetEncryptionPassword(string password)
        {
            globalEncryptionPassword = password;
        }

        /// <summary>
        /// Registra una función de migración manual entre versiones de una clase.
        /// </summary>
        public static void RegisterMigrator<T>(int fromVersion, Func<string, string> migrator)
        {
            MigrationManager.RegisterMigrator<T>(fromVersion, migrator);
        }

        /// <summary>
        /// Registra un convertidor personalizado estático para serializar/deserializar tipos.
        /// (útil para Listas, Arrays o primitivos de nivel superior, evitando reflexión en AOT/IL2CPP).
        /// </summary>
        public static void RegisterConverter<T>(Func<T, string> serializer, Func<string, T> deserializer)
        {
            CustomConverterManager.RegisterConverter(serializer, deserializer);
        }

        /// <summary>
        /// Guarda un objeto serializado bajo la clave especificada.
        /// </summary>
        public static void Save<T>(string key, T data, bool encrypt = false, bool compress = false)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentException("La clave no puede estar vacía.", nameof(key));
            if (data == null)
            {
                Debug.LogWarning($"[SaveSystem] Intentando guardar un valor nulo en la clave '{key}'. La operación fue ignorada.");
                return;
            }

            Type t = typeof(T);

            // Validar que no se intente serializar listas/arrays de nivel superior a menos que haya un convertidor.
            // Nota: Se permiten Structs (IsValueType) excepto los primitivos y enums del sistema, ya que JsonUtility puede serializar structs personalizados.
            bool isUnsupportedRoot = (typeof(IEnumerable).IsAssignableFrom(t) && t != typeof(string)) ||
                                     t.IsPrimitive ||
                                     t.IsEnum ||
                                     t == typeof(string) ||
                                     t == typeof(decimal) ||
                                     t == typeof(DateTime) ||
                                     t == typeof(Guid);

            if (isUnsupportedRoot)
            {
                if (!CustomConverterManager.HasConverter(t))
                {
                    Debug.LogError($"[SaveSystem] Error en clave '{key}'. Unity JsonUtility no serializa nativamente arreglos, listas, enums o primitivos ({t.Name}) en el nivel superior. Envuelve tus datos en una clase/struct, o usa SaveSystem.RegisterConverter<T>().");
                    return;
                }
            }

            int currentVersion = MigrationManager.GetCurrentVersion(typeof(T));
            string json;

            if (CustomConverterManager.HasConverter(t))
            {
                json = CustomConverterManager.Serialize(t, data);
            }
            else
            {
                json = JsonUtility.ToJson(data);
                // Validar silenciosamente la serialización de Unity
                if (json == "{}")
                {
                     // Es un heurístico de advertencia para tipos complejos no serializables nativamente.
                     Debug.LogWarning($"[SaveSystem] La serialización de '{key}' resultó en '{{}}'. Asegúrate de que tu clase '{t.Name}' usa campos públicos o [SerializeField].");
                }
            }

            byte[] payload = Encoding.UTF8.GetBytes(json);

            byte flags = 0;

            if (compress)
            {
                payload = GZipCompression.Compress(payload);
                flags |= FLAG_COMPRESSED;
            }

            if (encrypt)
            {
                if (string.IsNullOrEmpty(globalEncryptionPassword))
                {
                    Debug.LogWarning("[SaveSystem] Se solicitó encriptación pero la contraseña global está vacía. Guardando sin encriptar.");
                }
                else
                {
                    payload = AESEncryption.Encrypt(payload, globalEncryptionPassword);
                    flags |= FLAG_ENCRYPTED;
                }
            }

            // Metadatos: 4 bytes
            // [0] = Flags (encriptado, comprimido)
            // [1] = Reservado
            // [2] = Version (High byte)
            // [3] = Version (Low byte)
            byte[] header = new byte[4];
            header[0] = flags;
            header[1] = 0;
            header[2] = (byte)(currentVersion >> 8);
            header[3] = (byte)(currentVersion & 0xFF);

            // Combina cabecera y payload
            byte[] finalData = new byte[header.Length + payload.Length];
            Array.Copy(header, 0, finalData, 0, header.Length);
            Array.Copy(payload, 0, finalData, header.Length, payload.Length);

            currentBackend.Save(key, finalData);
        }

        /// <summary>
        /// Carga los datos almacenados bajo la clave especificada. Retorna default si no existe.
        /// Resuelve encriptación, compresión y migración de versión automáticamente en base a sus metadatos internos.
        /// </summary>
        public static T Load<T>(string key)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentException("La clave no puede estar vacía.", nameof(key));

            byte[] rawData = currentBackend.Load(key);
            if (rawData == null || rawData.Length < 4) return default(T);

            // Leer cabecera (metadatos)
            byte flags = rawData[0];
            int savedVersion = (rawData[2] << 8) | rawData[3];

            bool isEncrypted = (flags & FLAG_ENCRYPTED) != 0;
            bool isCompressed = (flags & FLAG_COMPRESSED) != 0;

            byte[] payload = new byte[rawData.Length - 4];
            Array.Copy(rawData, 4, payload, 0, payload.Length);

            if (isEncrypted)
            {
                if (string.IsNullOrEmpty(globalEncryptionPassword))
                {
                    Debug.LogError($"[SaveSystem] Los datos de '{key}' están encriptados pero no se configuró una contraseña global. Imposible cargar.");
                    return default(T);
                }
                try
                {
                    payload = AESEncryption.Decrypt(payload, globalEncryptionPassword);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[SaveSystem] Error al desencriptar la clave '{key}'. ¿Contraseña incorrecta o datos corruptos? {e.Message}");
                    return default(T);
                }
            }

            if (isCompressed)
            {
                try
                {
                    payload = GZipCompression.Decompress(payload);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[SaveSystem] Error al descomprimir la clave '{key}'. Archivo corrupto. {e.Message}");
                    return default(T);
                }
            }

            string json = Encoding.UTF8.GetString(payload);
            Type t = typeof(T);

            // Validar migración de versiones
            int currentVersion = MigrationManager.GetCurrentVersion(t);
            if (savedVersion < currentVersion)
            {
                json = MigrationManager.MigrateIfNeeded<T>(json, savedVersion, currentVersion);
            }

            // Deserialización final
            try
            {
                if (CustomConverterManager.HasConverter(t))
                {
                    return (T)CustomConverterManager.Deserialize(t, json);
                }
                else
                {
                    return JsonUtility.FromJson<T>(json);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Error al deserializar JSON de la clave '{key}'. {e.Message}");
                return default(T);
            }
        }

        // Métodos de conveniencia (Alias más cortos)
        public static void Set<T>(string key, T data, bool encrypt = false, bool compress = false) => Save(key, data, encrypt, compress);
        public static T Get<T>(string key) => Load<T>(key);

        public static void Delete(string key) => currentBackend.Delete(key);
        public static bool HasKey(string key) => currentBackend.HasKey(key);
        public static void DeleteAll() => currentBackend.DeleteAll();

        /// <summary>
        /// Obtiene todas las claves almacenadas en el backend actual.
        /// </summary>
        public static string[] GetAllKeys()
        {
            var keys = currentBackend.GetAllKeys();
            if (keys == null) return new string[0];

            // Conversión de IEnumerable a Array para mayor simplicidad en el uso general
            System.Collections.Generic.List<string> list = new System.Collections.Generic.List<string>(keys);
            return list.ToArray();
        }

        /// <summary>
        /// Retorna la instancia del backend actual que SaveSystem está usando.
        /// </summary>
        public static ISaveBackend GetCurrentBackend() => currentBackend;
    }
}
