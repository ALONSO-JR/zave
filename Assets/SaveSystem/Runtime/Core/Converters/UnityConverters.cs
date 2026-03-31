using System;
using UnityEngine;

namespace SaveSystemUltimate.Core.Converters
{
    /// <summary>
    /// Proporciona convertidores predeterminados para los tipos primitivos y nativos de Unity
    /// que `JsonUtility` no puede serializar nativamente en la raíz (ej: Vector3).
    /// Se auto-registran al cargar el juego para que el usuario no tenga que configurarlos manualmente.
    /// </summary>
    public static class UnityConverters
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoRegister()
        {
            // Primitivos del sistema y cadenas (JsonUtility requiere un envoltorio para ellos a nivel raíz)
            RegisterPrimitive<int>();
            RegisterPrimitive<float>();
            RegisterPrimitive<bool>();
            RegisterPrimitive<string>();
            RegisterPrimitive<long>();
            RegisterPrimitive<double>();

            // Tipos de Unity (que son ValueTypes o clases pero fallan como objeto raíz en JsonUtility)
            RegisterPrimitive<Vector2>();
            RegisterPrimitive<Vector3>();
            RegisterPrimitive<Vector4>();
            RegisterPrimitive<Quaternion>();
            RegisterPrimitive<Color>();
            RegisterPrimitive<Color32>();
            RegisterPrimitive<Rect>();
            RegisterPrimitive<RectOffset>();
            RegisterPrimitive<Bounds>();
            RegisterPrimitive<Matrix4x4>();
            RegisterPrimitive<Vector2Int>();
            RegisterPrimitive<Vector3Int>();
            RegisterPrimitive<RangeInt>();

            // Para tipos complejos como AnimationCurve o Gradient, usar un envoltorio serializable también es lo ideal.
            RegisterPrimitive<Gradient>();
            RegisterPrimitive<AnimationCurve>();
        }

        /// <summary>
        /// Crea y registra un convertidor genérico usando un envoltorio (PrimitiveWrapper)
        /// que es procesado internamente por JsonUtility.
        /// </summary>
        private static void RegisterPrimitive<T>()
        {
            CustomConverterManager.RegisterConverter<T>(
                serializer: (value) =>
                {
                    var wrapper = new PrimitiveWrapper<T>(value);
                    return JsonUtility.ToJson(wrapper);
                },
                deserializer: (json) =>
                {
                    var wrapper = JsonUtility.FromJson<PrimitiveWrapper<T>>(json);
                    return wrapper != null ? wrapper.Value : default(T);
                }
            );
        }
    }

    /// <summary>
    /// Envoltura serializable genérica utilizada internamente por los UnityConverters.
    /// </summary>
    [Serializable]
    public class PrimitiveWrapper<T>
    {
        public T Value;

        public PrimitiveWrapper() { }

        public PrimitiveWrapper(T value)
        {
            Value = value;
        }
    }
}
