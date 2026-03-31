# SaveSystem Ultimate

SaveSystem Ultimate es el sistema de guardado más completo, fácil de usar y potente para Unity. Diseñado para superar a Easy Save, Crystal Save y Bayat Save System, ofrece todas las funcionalidades de grado profesional en un único paquete totalmente gratuito. Compatible con IL2CPP y todas las plataformas (incluyendo WebGL, iOS, Android, Windows y Mac). Su API es ridículamente simple: una línea para guardar, una línea para cargar.

## Características Principales

1. **Serialización Universal Compatible con AOT/IL2CPP**: Basado en `JsonUtility` para evitar el uso intensivo de reflexión, garantizando compatibilidad en dispositivos móviles, WebGL y consolas. Soporta nativamente los tipos más comunes de Unity y permite la creación de convertidores personalizados.
2. **Múltiples Backends Intercambiables**:
   - `FileBackend`: Guarda archivos en el almacenamiento persistente (`.sav`).
   - `PlayerPrefsBackend`: Ideal para configuraciones pequeñas y como backend principal para **WebGL**, ya que en Unity moderno persiste nativamente en IndexedDB.
   - `MemoryBackend`: Almacena datos temporalmente en RAM (muy útil para pruebas).
3. **Seguridad Integrada**: Encriptación AES y compresión GZip nativa. Protege los archivos de guardado y reduce su tamaño al vuelo, guardando un archivo binario o un texto legible según tus necesidades.
4. **Editor Visual Potente**: Una ventana completa (vía `Window > Save System Manager`) que te permite inspeccionar las claves guardadas, ver su contenido formateado (como JSON o representación hex de bytes), exportarlas o eliminarlas.
5. **Migración Automática de Datos**: Con el atributo `[SaveVersion]`, el sistema permite registrar funciones de migración. Así, puedes actualizar el esquema de datos de tu juego sin perder las partidas de tus jugadores.
6. **Alto Rendimiento y Asincronía**: Ofrece versiones asíncronas de todos los métodos principales (`SaveAsync`, `LoadAsync`, etc.), usando `Task.Run` para no bloquear el hilo principal durante la serialización/deserialización y operaciones de E/S.
7. **Soporte de Colecciones Complejas**: Incluye `SerializableDictionary` para superar las limitaciones de `JsonUtility` con diccionarios.

## Instalación

### Opción 1: A través de Unity Package Manager (UPM)
1. Abre Unity y ve a `Window > Package Manager`.
2. Haz clic en el botón `+` en la esquina superior izquierda y selecciona `Add package from disk...`.
3. Navega hasta la carpeta del paquete y selecciona el archivo `package.json`.

### Opción 2: Instalación Manual
Simplemente copia toda la carpeta `Assets/SaveSystem` dentro del directorio `Assets` de tu proyecto Unity.

## Guía de Inicio Rápido

### 1. Configuración Inicial (Opcional)
Puedes definir la contraseña de encriptación globalmente y cambiar el backend de forma global si lo necesitas (por defecto es `FileBackend`).

```csharp
using SaveSystemUltimate;

// Configurar la contraseña para encriptar los datos
SaveSystem.SetEncryptionPassword("SuperSecretPassword123!");

// Cambiar el backend (por ejemplo, a PlayerPrefs, ideal para WebGL)
SaveSystem.SetBackend(new PlayerPrefsBackend());
```

### 2. Guardado de Datos
La API es extremadamente simple. Aquí tienes algunos ejemplos:

```csharp
PlayerData data = new PlayerData { level = 10, health = 100f };

// Guardado básico
SaveSystem.Save("player_data", data);

// Guardado con encriptación y compresión
SaveSystem.Save("player_data_secure", data, encrypt: true, compress: true);

// Asincrónico
await SaveSystem.SaveAsync("player_data_async", data, encrypt: true);
```

### 3. Carga de Datos
Cargar datos es igual de sencillo. Si los datos fueron guardados con encriptación o compresión, el sistema detecta sus metadatos internos y los procesa automáticamente.

```csharp
// Carga básica (retorna default si no existe)
PlayerData data = SaveSystem.Load<PlayerData>("player_data");

if (data != null) {
    Debug.Log($"Jugador cargado: Nivel {data.level}, Salud: {data.health}");
}

// Asincrónico
PlayerData asyncData = await SaveSystem.LoadAsync<PlayerData>("player_data_async");
```

### 4. Uso de Diccionarios y Colecciones
Debido a las limitaciones de `JsonUtility`, Unity no puede serializar diccionarios de forma nativa. Hemos incluido `SerializableDictionary` para solucionarlo.

```csharp
[Serializable]
public class Inventory
{
    public SerializableDictionary<string, int> items = new SerializableDictionary<string, int>();
}

Inventory myInventory = new Inventory();
myInventory.items.Add("potion", 5);
myInventory.items.Add("sword", 1);

SaveSystem.Save("inventory", myInventory);
```

### 5. Migración de Datos (Versionado)
Si añades o cambias campos en una clase a medida que actualizas tu juego, puedes usar el sistema de migración.

Añade el atributo `[SaveVersion]` a tu clase y registra un método de migración:

```csharp
[Serializable, SaveVersion(2)]
public class PlayerData
{
    public int level;
    public float maxHealth; // Nuevo campo en versión 2
}

// En la inicialización del juego:
SaveSystem.RegisterMigrator<PlayerData>(1, (oldJson) => {
    // Convierte el JSON de la versión 1 a la versión 2
    var oldData = JsonUtility.FromJson<PlayerDataV1>(oldJson);
    var newData = new PlayerData();
    newData.level = oldData.level;
    newData.maxHealth = 100f; // Asigna un valor predeterminado para el nuevo campo
    return JsonUtility.ToJson(newData);
});
```

### 6. Inspección de Archivos Visual
Abre la ventana en `Window > Save System Manager`. Esta ventana te permite:
- Ver una lista de todas las claves que están almacenadas en el backend actual.
- Cambiar temporalmente el backend desde la interfaz para inspeccionar otros lugares (por ejemplo, ver qué hay en memoria o en PlayerPrefs).
- Seleccionar una clave y ver su contenido (en JSON formateado o en versión Hex si está encriptado/comprimido).
- Eliminar, exportar como `.json` o importar un archivo previamente exportado.

## Arquitectura y Módulos
- **`ISaveBackend`**: Define la interfaz de lectura/escritura a bytes. Puedes crear tu propio backend (ej. `GoogleDriveBackend`) simplemente implementando esta interfaz y usándolo a través de `SaveSystem.SetBackend()`.
- **Formato y Metadatos**: El sistema añade una cabecera de 4 bytes a los datos antes de guardarlos. Esta cabecera es un _magic number_ (una máscara de bits) que indica si el archivo original fue encriptado, comprimido o guardado como texto en crudo. Esto hace que la carga no requiera saber los parámetros iniciales; el sistema sabe exactamente cómo revertir el proceso.

## Solución de Problemas

**Q: ¿Por qué en WebGL mis datos no se guardan entre sesiones?**
A: Unity (especialmente en versiones antiguas) tiene problemas con `Application.persistentDataPath` en el navegador. Por eso se recomienda configurar `SaveSystem.SetBackend(new PlayerPrefsBackend());` si detectas que la plataforma es WebGL (`Application.platform == RuntimePlatform.WebGLPlayer`). Las versiones recientes de Unity guardan `PlayerPrefs` directamente en IndexedDB de forma persistente y rápida.

**Q: ¿Puedo usar `SaveSystem` con mis propios convertidores JSON como Newtonsoft?**
A: `SaveSystem` está fuertemente atado a `JsonUtility` para garantizar compatibilidad, seguridad de tipos AOT e IL2CPP, y alta velocidad. Si tu clase no es compatible con `JsonUtility`, considera usar el sistema de variables y diccionarios envolventes (como el `SerializableDictionary` incluido) o crea una clase de transferencia de datos de guardado temporal (Data Transfer Object) con tipos más simples.

### Serialización de Listas y Arreglos

Unity `JsonUtility` **no permite** serializar listas o arreglos de nivel superior (ej. `List<int>`). Si intentas hacer `SaveSystem.Save("miLista", miLista)`, el sistema bloqueará la operación con un error para evitar pérdidas silenciosas de datos.

**Solución 1: Envolver en una clase (Recomendado)**
```csharp
[Serializable]
public class ListWrapper {
    public List<int> miLista = new List<int>();
}
```

**Solución 2: Usar Convertidores Personalizados (Ideal para IL2CPP)**
Si necesitas guardar un tipo externo, un primitivo simple o una colección en el nivel superior sin usar una clase envoltorio, puedes registrar tus propios convertidores estáticos:

```csharp
// Registramos cómo serializar y deserializar una lista de enteros en formato CSV, por ejemplo.
SaveSystem.RegisterConverter<List<int>>(
    (lista) => string.Join(",", lista),
    (json) => new List<int>(Array.ConvertAll(json.Split(','), int.Parse))
);

// Ahora el guardado funcionará perfectamente
SaveSystem.Save("miLista", miLista);
```
