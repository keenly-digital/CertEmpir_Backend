using System.Security.Cryptography;
using System.Text;

namespace EncryptionDecryptionUsingSymmetricKey
{
    public class AesOperation
    {
        private const int KeySize = 32; // AES-256
        private const int IvSize = 16;  // AES Block Size (128-bit)

        public string EncryptString(string key, string plainText)
        {
            byte[] iv = new byte[IvSize];
            byte[] array;

            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(key.PadRight(KeySize).Substring(0, KeySize)); // Ensure correct key size
                aes.IV = iv;

                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                using (MemoryStream memoryStream = new MemoryStream())
                {
                    using (CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter streamWriter = new StreamWriter(cryptoStream))
                        {
                            streamWriter.Write(plainText);
                        }

                        array = memoryStream.ToArray();
                    }
                }
            }

            return Convert.ToBase64String(array); // Ensure Base64 encoding
        }

        public string DecryptString(string key, string cipherText)
        {
            byte[] iv = new byte[IvSize];
            byte[] buffer = Convert.FromBase64String(cipherText); // Always decode from Base64

            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(key.PadRight(KeySize).Substring(0, KeySize)); // Ensure correct key size
                aes.IV = iv;
                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                using (MemoryStream memoryStream = new MemoryStream(buffer))
                {
                    using (CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader streamReader = new StreamReader(cryptoStream))
                        {
                            return streamReader.ReadToEnd();
                        }
                    }
                }
            }
        }
    }
}
