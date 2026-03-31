using System.Collections.Generic;

namespace SaveSystemUltimate.Core
{
    /// <summary>
    /// Define la interfaz que cualquier backend de almacenamiento debe implementar.
    /// Permite guardar, cargar, eliminar y listar claves de forma agnóstica a la ubicación (disco, memoria, nube, etc.).
    /// </summary>
    public interface ISaveBackend
    {
        /// <summary>
        /// Guarda un arreglo de bytes bajo una clave.
        /// </summary>
        void Save(string key, byte[] data);

        /// <summary>
        /// Carga los datos almacenados en la clave. Retorna null si no existe.
        /// </summary>
        byte[] Load(string key);

        /// <summary>
        /// Elimina los datos asociados a la clave.
        /// </summary>
        void Delete(string key);

        /// <summary>
        /// Verifica si existe información almacenada bajo la clave.
        /// </summary>
        bool HasKey(string key);

        /// <summary>
        /// Obtiene una lista de todas las claves almacenadas en este backend.
        /// </summary>
        IEnumerable<string> GetAllKeys();

        /// <summary>
        /// Elimina todas las claves y datos en este backend.
        /// </summary>
        void DeleteAll();
    }
}
