using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Levrum.Utils
{
    public class AESCryptor
    {
        public string StringPermutation { get; set; } = "ChangeMe!";
        public byte BytePermutation1 { get; set; } = 0x19;
        public byte BytePermutation2 { get; set; } = 0x91;
        public byte BytePermutation3 { get; set; } = 0x25;
        public byte BytePermutation4 { get; set; } = 0x75;

        public string Encrypt(string data)
        {
            return Convert.ToBase64String(Encrypt(Encoding.UTF8.GetBytes(data)));
        }

        public string Decrypt(string data)
        {
            return Encoding.UTF8.GetString(Decrypt(Convert.FromBase64String(data)));
        }

        public byte[] Encrypt(byte[] data)
        {
            return processData(data, false);
        }

        public byte[] Decrypt(byte[] data)
        {
            return processData(data, true);
        }

        private byte[] processData(byte[] data, bool decrypt = false)
        {
            PasswordDeriveBytes bytes = new PasswordDeriveBytes(StringPermutation,
                new byte[] { BytePermutation1, BytePermutation2, BytePermutation3, BytePermutation4 });

            MemoryStream memStream = new MemoryStream();
            Aes aes = new AesManaged();
            aes.Key = bytes.GetBytes(aes.KeySize / 8);
            aes.IV = bytes.GetBytes(aes.BlockSize / 8);

            ICryptoTransform transform = decrypt ? aes.CreateDecryptor() : aes.CreateEncryptor();
            CryptoStream cryptoStream = new CryptoStream(memStream, transform, CryptoStreamMode.Write);

            cryptoStream.Write(data, 0, data.Length);
            cryptoStream.Close();

            return memStream.ToArray();
        }
    }
}
