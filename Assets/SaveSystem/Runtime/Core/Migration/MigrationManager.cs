using System;
using System.Collections.Generic;

namespace SaveSystemUltimate.Core.Migration
{
    /// <summary>
    /// Gestiona las funciones de migración registradas por el usuario.
    /// Esto permite pasar un string JSON antiguo y convertirlo (manualmente) al JSON esperado por la nueva versión.
    /// </summary>
    public static class MigrationManager
    {
        // Almacena diccionarios con la versión de origen como clave, y un delegado que transforma string(json) -> string(json)
        private static readonly Dictionary<Type, Dictionary<int, Func<string, string>>> Migrators = new Dictionary<Type, Dictionary<int, Func<string, string>>>();

        /// <summary>
        /// Registra una función de migración que convierte el JSON de una versión antigua al JSON de la versión más reciente.
        /// </summary>
        /// <typeparam name="T">El tipo de la clase actual (destino de la migración).</typeparam>
        /// <param name="fromVersion">La versión antigua desde la que se migra (ej: 1).</param>
        /// <param name="migrator">Una función que toma el JSON antiguo y devuelve el nuevo JSON válido para la versión actual.</param>
        public static void RegisterMigrator<T>(int fromVersion, Func<string, string> migrator)
        {
            var type = typeof(T);
            if (!Migrators.ContainsKey(type))
            {
                Migrators[type] = new Dictionary<int, Func<string, string>>();
            }

            Migrators[type][fromVersion] = migrator;
        }

        /// <summary>
        /// Obtiene la versión actual de la clase basada en el atributo [SaveVersion]. Si no lo tiene, asume 1.
        /// </summary>
        public static int GetCurrentVersion(Type type)
        {
            var attributes = type.GetCustomAttributes(typeof(SaveVersionAttribute), false);
            if (attributes.Length > 0)
            {
                return ((SaveVersionAttribute)attributes[0]).Version;
            }
            return 1;
        }

        /// <summary>
        /// Si hay un migrador registrado para la versión de origen especificada, lo aplica.
        /// De lo contrario, se devuelve el JSON original y se confía en que JsonUtility haga el mejor esfuerzo de mapeo.
        /// </summary>
        public static string MigrateIfNeeded<T>(string originalJson, int savedVersion, int currentVersion)
        {
            var type = typeof(T);

            if (savedVersion < currentVersion && Migrators.ContainsKey(type))
            {
                var typeMigrators = Migrators[type];

                // Si encontramos un migrador exacto para la versión guardada, lo usamos.
                if (typeMigrators.ContainsKey(savedVersion))
                {
                    return typeMigrators[savedVersion](originalJson);
                }
            }

            // Si no hay migrador, simplemente devolvemos el original
            return originalJson;
        }
    }
}
