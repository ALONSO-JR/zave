using System;
using System.Threading.Tasks;
using UnityEngine;

namespace SaveSystemUltimate.Core.Async
{
    /// <summary>
    /// Proporciona versiones asíncronas de los métodos principales de SaveSystem para evitar
    /// bloquear el hilo principal durante operaciones pesadas (serialización y I/O).
    /// </summary>
    public static class SaveSystemAsync
    {
        /// <summary>
        /// Guarda un objeto serializado bajo la clave especificada de manera asíncrona.
        /// </summary>
        public static async Task SaveAsync<T>(string key, T data, bool encrypt = false, bool compress = false)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentException("La clave no puede estar vacía.", nameof(key));

#if UNITY_WEBGL && !UNITY_EDITOR
            // WebGL no soporta el ThreadPool de .NET nativamente (Task.Run falla con PlatformNotSupportedException).
            // Para WebGL ejecutamos sincrónicamente (ya que PlayerPrefs es extremadamente rápido en memoria local)
            // o se recomendaría integrar UniTask si se desea emular yields por frame.
            try
            {
                SaveSystem.Save(key, data, encrypt, compress);
                await Task.Yield(); // Permite liberar el control brevemente en el hilo principal
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystemAsync] Error al guardar en WebGL '{key}': {e.Message}");
                throw;
            }
#else
            // En otras plataformas, Task.Run se encarga de serializar, comprimir, encriptar y escribir en un hilo de fondo.
            await Task.Run(() =>
            {
                try
                {
                    SaveSystem.Save(key, data, encrypt, compress);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[SaveSystemAsync] Error al guardar asíncronamente '{key}': {e.Message}");
                    throw; // Propaga la excepción si el usuario desea capturarla con await
                }
            });
#endif
        }

        /// <summary>
        /// Carga los datos almacenados bajo la clave especificada de manera asíncrona.
        /// </summary>
        public static async Task<T> LoadAsync<T>(string key)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentException("La clave no puede estar vacía.", nameof(key));

#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                var result = SaveSystem.Load<T>(key);
                await Task.Yield();
                return result;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystemAsync] Error al cargar en WebGL '{key}': {e.Message}");
                throw;
            }
#else
            // Task.Run se encarga de leer, desencriptar, descomprimir y deserializar en un hilo de fondo.
            return await Task.Run(() =>
            {
                try
                {
                    return SaveSystem.Load<T>(key);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[SaveSystemAsync] Error al cargar asíncronamente '{key}': {e.Message}");
                    throw;
                }
            });
#endif
        }

        /// <summary>
        /// Guarda asíncronamente con Callbacks (para aquellos que prefieren eventos en lugar de Task/await).
        /// </summary>
        public static void SaveWithCallback<T>(string key, T data, bool encrypt, bool compress, Action onSuccess, Action<string> onError)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                SaveSystem.Save(key, data, encrypt, compress);
                onSuccess?.Invoke();
            }
            catch (Exception e)
            {
                onError?.Invoke(e.Message);
            }
#else
            Task.Run(() =>
            {
                try
                {
                    SaveSystem.Save(key, data, encrypt, compress);
                    // Como estamos en un hilo de fondo, la forma segura en Unity puro es usar un Dispatcher si existe.
                    // Para simplificar, invocamos directamente (cuidado al modificar UI desde aquí).
                    onSuccess?.Invoke();
                }
                catch (Exception e)
                {
                    onError?.Invoke(e.Message);
                }
            });
#endif
        }

        /// <summary>
        /// Carga asíncronamente con Callbacks (para aquellos que prefieren eventos en lugar de Task/await).
        /// </summary>
        public static void LoadWithCallback<T>(string key, Action<T> onSuccess, Action<string> onError)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                T result = SaveSystem.Load<T>(key);
                onSuccess?.Invoke(result);
            }
            catch (Exception e)
            {
                onError?.Invoke(e.Message);
            }
#else
            Task.Run(() =>
            {
                try
                {
                    T result = SaveSystem.Load<T>(key);
                    onSuccess?.Invoke(result);
                }
                catch (Exception e)
                {
                    onError?.Invoke(e.Message);
                }
            });
#endif
        }
    }
}
