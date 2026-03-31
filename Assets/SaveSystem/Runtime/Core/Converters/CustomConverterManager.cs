using System;
using System.Collections.Generic;
using UnityEngine;

namespace SaveSystemUltimate.Core.Converters
{
    /// <summary>
    /// Gestiona los convertidores personalizados para tipos que JsonUtility no soporta
    /// nativamente (por ejemplo, tipos primitivos en el nivel raíz, arreglos, o clases externas).
    /// Permite al usuario registrar funciones estáticas de conversión a JSON y desde JSON,
    /// garantizando compatibilidad total con AOT e IL2CPP sin requerir reflexión.
    /// </summary>
    public static class CustomConverterManager
    {
        private static readonly Dictionary<Type, Func<object, string>> Serializers = new Dictionary<Type, Func<object, string>>();
        private static readonly Dictionary<Type, Func<string, object>> Deserializers = new Dictionary<Type, Func<string, object>>();

        /// <summary>
        /// Registra un convertidor personalizado para un tipo específico.
        /// </summary>
        /// <typeparam name="T">El tipo a convertir.</typeparam>
        /// <param name="serializeFunc">Função que convierte el objeto a un string (JSON o formato propio).</param>
        /// <param name="deserializeFunc">Função que convierte el string al objeto original.</param>
        public static void RegisterConverter<T>(Func<T, string> serializeFunc, Func<string, T> deserializeFunc)
        {
            var type = typeof(T);
            Serializers[type] = (obj) => serializeFunc((T)obj);
            Deserializers[type] = (json) => deserializeFunc(json);
        }

        /// <summary>
        /// Verifica si hay un convertidor registrado para un tipo.
        /// </summary>
        public static bool HasConverter(Type type)
        {
            return Serializers.ContainsKey(type) && Deserializers.ContainsKey(type);
        }

        /// <summary>
        /// Serializa un objeto usando su convertidor registrado.
        /// </summary>
        public static string Serialize(Type type, object obj)
        {
            if (Serializers.TryGetValue(type, out var serializer))
            {
                return serializer(obj);
            }
            throw new InvalidOperationException($"No hay un convertidor registrado para el tipo {type}.");
        }

        /// <summary>
        /// Deserializa un string a un objeto usando su convertidor registrado.
        /// </summary>
        public static object Deserialize(Type type, string json)
        {
            if (Deserializers.TryGetValue(type, out var deserializer))
            {
                return deserializer(json);
            }
            throw new InvalidOperationException($"No hay un convertidor registrado para el tipo {type}.");
        }
    }
}
