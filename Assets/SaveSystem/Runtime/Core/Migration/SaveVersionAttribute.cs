using System;

namespace SaveSystemUltimate.Core.Migration
{
    /// <summary>
    /// Un atributo opcional que se coloca encima de las clases serializables.
    /// Define la versión actual del esquema de datos.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class SaveVersionAttribute : Attribute
    {
        public int Version { get; }

        public SaveVersionAttribute(int version)
        {
            Version = version;
        }
    }
}
