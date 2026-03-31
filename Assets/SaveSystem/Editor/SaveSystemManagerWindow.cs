#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using SaveSystemUltimate.Core;
using SaveSystemUltimate.Core.Backends;

namespace SaveSystemUltimate.Editor
{
    /// <summary>
    /// Ventana de Editor para gestionar visualmente los datos guardados por SaveSystem.
    /// Permite inspeccionar, eliminar, exportar e importar datos.
    /// </summary>
    public class SaveSystemManagerWindow : EditorWindow
    {
        private int selectedBackendIndex = 0;
        private string[] backendNames = { "FileBackend", "PlayerPrefsBackend", "MemoryBackend" };
        private ISaveBackend[] backends;

        private string[] savedKeys = new string[0];
        private Vector2 keysScrollPos;
        private Vector2 contentScrollPos;

        private string selectedKey = null;
        private byte[] selectedData = null;
        private string selectedDataDisplay = "";
        private bool displayAsHex = false;

        [MenuItem("Window/Save System Manager")]
        public static void ShowWindow()
        {
            GetWindow<SaveSystemManagerWindow>("Save System Manager");
        }

        private void OnEnable()
        {
            // Inicializamos una instancia de cada backend para inspeccionarlos libremente
            backends = new ISaveBackend[]
            {
                new FileBackend(),
                new PlayerPrefsBackend(),
                new MemoryBackend()
            };

            RefreshKeys();
        }

        private void OnGUI()
        {
            GUILayout.Label("Gestor de SaveSystem Ultimate", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            DrawToolbar();
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();

            DrawKeysList();
            DrawContentPanel();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            GUILayout.Label("Backend actual:", GUILayout.Width(100));
            int newIndex = EditorGUILayout.Popup(selectedBackendIndex, backendNames, EditorStyles.toolbarPopup, GUILayout.Width(150));

            if (newIndex != selectedBackendIndex)
            {
                selectedBackendIndex = newIndex;
                ClearSelection();
                RefreshKeys();
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Refrescar", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                RefreshKeys();
                if (selectedKey != null) LoadSelectedKey();
            }

            if (GUILayout.Button("Borrar TODO", EditorStyles.toolbarButton, GUILayout.Width(90)))
            {
                if (EditorUtility.DisplayDialog("Confirmar borrado", $"¿Estás seguro de que quieres borrar TODAS las claves de {backendNames[selectedBackendIndex]}?", "Sí, Borrar", "Cancelar"))
                {
                    backends[selectedBackendIndex].DeleteAll();
                    ClearSelection();
                    RefreshKeys();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawKeysList()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(250));
            GUILayout.Label("Claves guardadas", EditorStyles.boldLabel);

            if (savedKeys.Length == 0)
            {
                GUILayout.Label("No hay claves guardadas.", EditorStyles.helpBox);
            }
            else
            {
                keysScrollPos = EditorGUILayout.BeginScrollView(keysScrollPos, "box");

                foreach (var key in savedKeys)
                {
                    GUIStyle style = (key == selectedKey) ? EditorStyles.selectionRect : EditorStyles.label;
                    if (GUILayout.Button(key, style))
                    {
                        selectedKey = key;
                        LoadSelectedKey();
                    }
                }

                EditorGUILayout.EndScrollView();
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawContentPanel()
        {
            EditorGUILayout.BeginVertical("box");

            if (string.IsNullOrEmpty(selectedKey))
            {
                GUILayout.Label("Selecciona una clave para ver su contenido.", EditorStyles.centeredGreyMiniLabel);
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label($"Clave: {selectedKey}", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Exportar", EditorStyles.miniButton, GUILayout.Width(70)))
                {
                    ExportSelectedKey();
                }

                if (GUILayout.Button("Importar", EditorStyles.miniButton, GUILayout.Width(70)))
                {
                    ImportSelectedKey();
                }

                GUI.color = Color.red;
                if (GUILayout.Button("Eliminar", EditorStyles.miniButton, GUILayout.Width(70)))
                {
                    backends[selectedBackendIndex].Delete(selectedKey);
                    ClearSelection();
                    RefreshKeys();
                }
                GUI.color = Color.white;

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space();

                if (selectedData != null)
                {
                    GUILayout.Label($"Tamaño: {selectedData.Length} bytes");

                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Toggle(!displayAsHex, "Texto (Crudo)", EditorStyles.miniButtonLeft))
                    {
                        if (displayAsHex)
                        {
                            displayAsHex = false;
                            UpdateDisplay();
                        }
                    }
                    if (GUILayout.Toggle(displayAsHex, "Hexadecimal", EditorStyles.miniButtonRight))
                    {
                        if (!displayAsHex)
                        {
                            displayAsHex = true;
                            UpdateDisplay();
                        }
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.Space();

                    contentScrollPos = EditorGUILayout.BeginScrollView(contentScrollPos, GUILayout.ExpandHeight(true));
                    // Usar un campo de texto seleccionable pero de solo lectura (con TextArea se puede copiar)
                    EditorGUILayout.TextArea(selectedDataDisplay, GUILayout.ExpandHeight(true));
                    EditorGUILayout.EndScrollView();
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void RefreshKeys()
        {
            var keysList = backends[selectedBackendIndex].GetAllKeys();
            if (keysList == null) savedKeys = new string[0];
            else
            {
                var list = new System.Collections.Generic.List<string>(keysList);
                savedKeys = list.ToArray();
            }
            Repaint();
        }

        private void ClearSelection()
        {
            selectedKey = null;
            selectedData = null;
            selectedDataDisplay = "";
        }

        private void LoadSelectedKey()
        {
            if (string.IsNullOrEmpty(selectedKey)) return;
            selectedData = backends[selectedBackendIndex].Load(selectedKey);
            UpdateDisplay();
            GUI.FocusControl(null);
        }

        private void UpdateDisplay()
        {
            if (selectedData == null)
            {
                selectedDataDisplay = "Error al cargar datos.";
                return;
            }

            if (displayAsHex)
            {
                StringBuilder hex = new StringBuilder(selectedData.Length * 3);
                for (int i = 0; i < selectedData.Length; i++)
                {
                    hex.AppendFormat("{0:x2} ", selectedData[i]);
                    if ((i + 1) % 16 == 0) hex.AppendLine();
                }
                selectedDataDisplay = hex.ToString();
            }
            else
            {
                // Verificamos si los datos tienen el tamaño mínimo para incluir el encabezado de 4 bytes de metadatos.
                if (selectedData.Length >= 4)
                {
                    // Comprobamos la bandera de encriptación o compresión para dar feedback en el editor.
                    byte flags = selectedData[0];
                    bool isEncrypted = (flags & (1 << 0)) != 0;
                    bool isCompressed = (flags & (1 << 1)) != 0;

                    if (isEncrypted || isCompressed)
                    {
                        selectedDataDisplay = $"[Este archivo está {(isEncrypted ? "encriptado" : "")} {(isEncrypted && isCompressed ? "y" : "")} {(isCompressed ? "comprimido" : "")}. Se muestra basura binaria.]\n\n";
                        selectedDataDisplay += Encoding.UTF8.GetString(selectedData);
                    }
                    else
                    {
                        // Si es texto plano, omitimos los primeros 4 bytes de cabecera para no mostrar caracteres corruptos
                        selectedDataDisplay = Encoding.UTF8.GetString(selectedData, 4, selectedData.Length - 4);
                    }
                }
                else
                {
                    // Archivos demasiado cortos
                    selectedDataDisplay = Encoding.UTF8.GetString(selectedData);
                }
            }
        }

        private void ExportSelectedKey()
        {
            if (selectedData == null) return;
            string path = EditorUtility.SaveFilePanel("Exportar datos", "", selectedKey + ".sav", "sav");
            if (!string.IsNullOrEmpty(path))
            {
                File.WriteAllBytes(path, selectedData);
                Debug.Log($"Datos exportados a: {path}");
            }
        }

        private void ImportSelectedKey()
        {
            if (string.IsNullOrEmpty(selectedKey)) return;
            string path = EditorUtility.OpenFilePanel("Importar datos", "", "sav");
            if (!string.IsNullOrEmpty(path))
            {
                byte[] imported = File.ReadAllBytes(path);
                backends[selectedBackendIndex].Save(selectedKey, imported);
                LoadSelectedKey();
                Debug.Log($"Datos importados de: {path} a la clave '{selectedKey}'");
            }
        }
    }
}
#endif
