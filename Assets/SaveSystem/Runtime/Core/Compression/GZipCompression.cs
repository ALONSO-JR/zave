using System;
using System.IO;
using System.IO.Compression;

namespace SaveSystemUltimate.Core.Compression
{
    /// <summary>
    /// Proporciona métodos de extensión para comprimir y descomprimir arreglos de bytes usando GZip.
    /// Esto reduce el tamaño del almacenamiento para grandes conjuntos de datos JSON.
    /// </summary>
    public static class GZipCompression
    {
        /// <summary>
        /// Comprime el arreglo de bytes usando el algoritmo GZip.
        /// </summary>
        /// <param name="data">Arreglo de bytes sin comprimir.</param>
        /// <returns>Arreglo de bytes comprimido.</returns>
        public static byte[] Compress(byte[] data)
        {
            if (data == null || data.Length == 0) return data;

            using (MemoryStream ms = new MemoryStream())
            {
                using (GZipStream gzip = new GZipStream(ms, CompressionMode.Compress))
                {
                    gzip.Write(data, 0, data.Length);
                }
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Descomprime un arreglo de bytes comprimido con GZip.
        /// </summary>
        /// <param name="compressedData">El arreglo de bytes comprimido.</param>
        /// <returns>El arreglo de bytes original sin comprimir.</returns>
        public static byte[] Decompress(byte[] compressedData)
        {
            if (compressedData == null || compressedData.Length == 0) return compressedData;

            using (MemoryStream msCompressed = new MemoryStream(compressedData))
            using (GZipStream gzip = new GZipStream(msCompressed, CompressionMode.Decompress))
            using (MemoryStream msDecompressed = new MemoryStream())
            {
                gzip.CopyTo(msDecompressed);
                return msDecompressed.ToArray();
            }
        }
    }
}
