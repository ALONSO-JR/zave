using System;
using System.IO;
using System.Security.Cryptography;

namespace SaveSystemUltimate.Core.Encryption
{
    /// <summary>
    /// Proporciona métodos de encriptación y desencriptación AES seguras.
    /// Deriva la clave a partir de una contraseña utilizando Rfc2898DeriveBytes (PBKDF2).
    /// Genera un Salt y un IV aleatorios para cada guardado, anexándolos al payload
    /// para garantizar seguridad semántica (CBC) y evitar debilidades criptográficas.
    /// </summary>
    public static class AESEncryption
    {
        private const int SaltSize = 16;
        private const int IvSize = 16; // 128 bit block size for AES
        private const int KeySize = 32; // 256 bit key size for AES
        private const int Iterations = 10000;

        /// <summary>
        /// Encripta los datos usando AES-256-CBC y una contraseña.
        /// El formato de salida es: [16 bytes Salt] + [16 bytes IV] + [Datos encriptados].
        /// </summary>
        public static byte[] Encrypt(byte[] data, string password)
        {
            if (data == null || data.Length == 0) return data;
            if (string.IsNullOrEmpty(password)) throw new ArgumentException("La contraseña no puede estar vacía al encriptar.", nameof(password));

            byte[] salt = new byte[SaltSize];
            byte[] iv = new byte[IvSize];

            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(salt);
                rng.GetBytes(iv);
            }

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Mode = CipherMode.CBC;
                aesAlg.Padding = PaddingMode.PKCS7;
                aesAlg.IV = iv;

                using (var deriveBytes = new Rfc2898DeriveBytes(password, salt, Iterations))
                {
                    aesAlg.Key = deriveBytes.GetBytes(KeySize);
                }

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    // Escribimos el Salt y el IV al inicio del stream.
                    msEncrypt.Write(salt, 0, salt.Length);
                    msEncrypt.Write(iv, 0, iv.Length);

                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, aesAlg.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        csEncrypt.Write(data, 0, data.Length);
                        csEncrypt.FlushFinalBlock();
                    }
                    return msEncrypt.ToArray();
                }
            }
        }

        /// <summary>
        /// Desencripta los datos encriptados usando AES-256-CBC y la contraseña.
        /// Asume que los primeros 16 bytes son el Salt y los siguientes 16 el IV.
        /// </summary>
        public static byte[] Decrypt(byte[] cipherData, string password)
        {
            if (cipherData == null || cipherData.Length == 0) return cipherData;
            if (string.IsNullOrEmpty(password)) throw new ArgumentException("La contraseña no puede estar vacía al desencriptar.", nameof(password));

            if (cipherData.Length < SaltSize + IvSize)
            {
                throw new CryptographicException("El archivo es demasiado pequeño para contener el Salt y el IV de AES.");
            }

            byte[] salt = new byte[SaltSize];
            byte[] iv = new byte[IvSize];

            Array.Copy(cipherData, 0, salt, 0, SaltSize);
            Array.Copy(cipherData, SaltSize, iv, 0, IvSize);

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Mode = CipherMode.CBC;
                aesAlg.Padding = PaddingMode.PKCS7;
                aesAlg.IV = iv;

                using (var deriveBytes = new Rfc2898DeriveBytes(password, salt, Iterations))
                {
                    aesAlg.Key = deriveBytes.GetBytes(KeySize);
                }

                // Empezamos a leer el texto cifrado después del Salt y el IV.
                using (MemoryStream msDecrypt = new MemoryStream(cipherData, SaltSize + IvSize, cipherData.Length - (SaltSize + IvSize)))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, aesAlg.CreateDecryptor(), CryptoStreamMode.Read))
                    {
                        using (MemoryStream msPlain = new MemoryStream())
                        {
                            csDecrypt.CopyTo(msPlain);
                            return msPlain.ToArray();
                        }
                    }
                }
            }
        }
    }
}
