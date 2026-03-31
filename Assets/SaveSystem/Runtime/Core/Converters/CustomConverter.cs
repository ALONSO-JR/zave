using System;
using UnityEngine;

namespace SaveSystemUltimate.Core.Converters
{
    /// <summary>
    /// Este es un ejemplo de cómo puedes registrar un convertidor de datos personalizado
    /// para tipos que `JsonUtility` no soporta (como DateTime o clases externas sin código fuente).
    /// </summary>
    public static class CustomConverterExample
    {
        // Se llama automáticamente antes de cargar la primera escena.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterMyCustomConverters()
        {
            // Ejemplo 1: Registrar DateTime como un string en formato ISO-8601
            CustomConverterManager.RegisterConverter<DateTime>(
                serializer: (dateTime) => dateTime.ToString("O"),
                deserializer: (jsonString) => DateTime.Parse(jsonString)
            );

            // Ejemplo 2: Registrar una tupla (que JsonUtility ignora) como un string separado por comas
            CustomConverterManager.RegisterConverter<Tuple<int, string>>(
                serializer: (tuple) => $"{tuple.Item1},{tuple.Item2}",
                deserializer: (jsonString) =>
                {
                    var parts = jsonString.Split(',');
                    if (parts.Length == 2 && int.TryParse(parts[0], out int number))
                    {
                        return new Tuple<int, string>(number, parts[1]);
                    }
                    return null;
                }
            );

            Debug.Log("[SaveSystemUltimate] Convertidores personalizados registrados (DateTime, Tuple).");
        }
    }
}
