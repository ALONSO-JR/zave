using System;
using UnityEngine;
using SaveSystemUltimate.Core;
using SaveSystemUltimate.Core.Backends;
using SaveSystemUltimate.Core.Migration;

namespace SaveSystemUltimate.Examples
{
    /// <summary>
    /// Script de ejemplo que demuestra las funcionalidades de SaveSystem Ultimate.
    /// Para usarlo, simplemente añade este script a cualquier GameObject en una escena vacía.
    /// Generará su propia interfaz de usuario utilizando GUI.
    /// </summary>
    public class DemoSaveSystem : MonoBehaviour
    {
        private PlayerData playerData;
        private string logMessage = "Inicia el sistema. Guarda o carga datos.";

        // Opciones de guardado
        private bool useEncryption = false;
        private bool useCompression = false;

        private void Start()
        {
            // Opcional: Configurar la contraseña de encriptación globalmente
            SaveSystem.SetEncryptionPassword("MiSuperSecretaPassword123!");

            // Opcional: Registrar un migrador (Ejemplo: De versión 1 a 2)
            // Si el jugador tenía datos de la versión 1, esto asegura que el campo maxHealth se asigne por defecto en la v2.
            SaveSystem.RegisterMigrator<PlayerData>(1, (oldJson) =>
            {
                var oldData = JsonUtility.FromJson<PlayerDataV1>(oldJson);
                var newData = new PlayerData
                {
                    playerName = oldData.playerName,
                    level = oldData.level,
                    health = oldData.health,
                    maxHealth = 100f // Nuevo campo con valor por defecto
                };
                return JsonUtility.ToJson(newData);
            });

            // Inicializar datos por defecto
            playerData = new PlayerData();
        }

        private void OnGUI()
        {
            int btnWidth = 200;
            int btnHeight = 40;
            int margin = 10;
            int currentY = 20;

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            GUIStyle logStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 14,
                alignment = TextAnchor.UpperLeft,
                wordWrap = true
            };

            GUI.Label(new Rect(10, currentY, 400, 30), "SaveSystem Ultimate Demo", titleStyle);
            currentY += 40;

            // Panel de Opciones y Backends
            GUI.Box(new Rect(10, currentY, 400, 110), "Configuración Global");
            currentY += 30;

            useEncryption = GUI.Toggle(new Rect(20, currentY, 150, 20), useEncryption, "Usar Encriptación");
            currentY += 25;
            useCompression = GUI.Toggle(new Rect(20, currentY, 150, 20), useCompression, "Usar Compresión");
            currentY += 25;

            GUI.Label(new Rect(20, currentY, 100, 20), "Backend:");
            if (GUI.Button(new Rect(80, currentY, 90, 20), "File"))
            {
                SaveSystem.SetBackend(new FileBackend());
                Log("Backend cambiado a: FileBackend (Disco local)");
            }
            if (GUI.Button(new Rect(180, currentY, 100, 20), "PlayerPrefs"))
            {
                SaveSystem.SetBackend(new PlayerPrefsBackend());
                Log("Backend cambiado a: PlayerPrefsBackend (Ideal para WebGL)");
            }
            if (GUI.Button(new Rect(290, currentY, 90, 20), "Memory"))
            {
                SaveSystem.SetBackend(new MemoryBackend());
                Log("Backend cambiado a: MemoryBackend (Volátil en RAM)");
            }

            currentY += 50;

            // Datos actuales del jugador
            GUI.Box(new Rect(10, currentY, 400, 130), "Datos en Memoria Activos");
            currentY += 30;

            GUI.Label(new Rect(20, currentY, 100, 20), "Nombre:");
            playerData.playerName = GUI.TextField(new Rect(100, currentY, 200, 20), playerData.playerName);
            currentY += 25;

            GUI.Label(new Rect(20, currentY, 100, 20), "Nivel:");
            if (int.TryParse(GUI.TextField(new Rect(100, currentY, 200, 20), playerData.level.ToString()), out int newLevel))
                playerData.level = newLevel;
            currentY += 25;

            GUI.Label(new Rect(20, currentY, 100, 20), "Salud:");
            GUI.Label(new Rect(100, currentY, 200, 20), $"{playerData.health} / {playerData.maxHealth}");
            if (GUI.Button(new Rect(310, currentY, 80, 20), "Daño -10")) playerData.health -= 10;
            currentY += 40;

            // Acciones de Guardado/Carga
            if (GUI.Button(new Rect(10, currentY, btnWidth, btnHeight), "Guardar Datos (Sync)"))
            {
                SaveSystem.Save("player_demo", playerData, useEncryption, useCompression);
                Log("Datos guardados correctamente.");
            }

            if (GUI.Button(new Rect(10 + btnWidth + margin, currentY, btnWidth, btnHeight), "Cargar Datos (Sync)"))
            {
                var loadedData = SaveSystem.Load<PlayerData>("player_demo");
                if (loadedData != null)
                {
                    playerData = loadedData;
                    Log("Datos cargados correctamente.");
                }
                else
                {
                    Log("Error al cargar o la clave no existe.");
                }
            }
            currentY += btnHeight + margin;

            if (GUI.Button(new Rect(10, currentY, btnWidth, btnHeight), "Guardar Asíncrono"))
            {
                Log("Guardando...");
                // Wrapper async simple
                _ = GuardarAsincrono();
            }

            if (GUI.Button(new Rect(10 + btnWidth + margin, currentY, btnWidth, btnHeight), "Borrar Datos"))
            {
                SaveSystem.Delete("player_demo");
                Log("Clave eliminada.");
            }
            currentY += btnHeight + margin;

            // Consola / Log
            GUI.Label(new Rect(10, currentY, 410, 80), logMessage, logStyle);
        }

        private async System.Threading.Tasks.Task GuardarAsincrono()
        {
            await SaveSystemUltimate.Core.Async.SaveSystemAsync.SaveAsync("player_demo", playerData, useEncryption, useCompression);
            Log("Guardado asíncrono completado.");
        }

        private void Log(string msg)
        {
            logMessage = $"[{DateTime.Now:HH:mm:ss}] {msg}";
            Debug.Log(logMessage);
        }
    }

    /// <summary>
    /// Simula un esquema de datos viejo (Versión 1)
    /// </summary>
    [Serializable]
    public class PlayerDataV1
    {
        public string playerName = "Hero";
        public int level = 1;
        public float health = 100f;
    }

    /// <summary>
    /// Esquema de datos actual (Versión 2, incluye maxHealth)
    /// </summary>
    [Serializable, SaveVersion(2)]
    public class PlayerData
    {
        public string playerName = "Hero";
        public int level = 1;
        public float health = 100f;
        public float maxHealth = 100f; // Campo añadido en v2
    }
}
